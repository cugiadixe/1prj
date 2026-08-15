import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from 'react';
import axiosClient from '../api/axiosClient';
import { CompanyContext } from './companyContext';
import {
  apiFetchMyPermissions,
  apiLogin,
  apiLogout,
  apiRefresh,
} from './authApi';
import type { CurrentUserPermissionDto, LoginRequest, LoginUserInfo } from './authApi';
import {
  clearAuthState,
  getAuthState,
  setAuthState,
} from './authState';

export interface AuthContextValue {
  isAuthenticated: boolean;
  mustChangePassword: boolean;
  user: LoginUserInfo | null;
  isBootstrapping: boolean;
  permissions: CurrentUserPermissionDto[];
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  onPasswordChanged: () => void;
  refreshPermissions: (companyId?: number) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

/**
 * Kiểm quyền theo mô hình phạm vi mới.
 *
 * Chữ ký cũ là `hasPermission(code, scope, companyId)` trong đó `scope` là chuỗi 'GLOBAL' /
 * 'COMPANY' mà nơi gọi phải TỰ ĐOÁN cho khớp data_scope của danh mục. Đoán sai thì phép kiểm
 * lặng lẽ trả false — 9 mã đang đoán sai, làm chết cả nhóm menu Thanh toán và ~12 nút hành động.
 * Nay bỏ hẳn tham số đó: phạm vi là dữ liệu do backend trả về, không phải thứ giao diện tự khai.
 *
 * Mặc định kiểm theo CÔNG TY ĐANG CHỌN. Khi chưa chọn công ty nào thì kiểm theo nghĩa
 * "có quyền này ở đâu đó" — để menu không biến mất trong lúc chờ chọn công ty.
 */
export function usePermissions() {
  const { permissions } = useAuth();
  // Không dùng useCompany() vì hook đó ném lỗi khi thiếu provider; ở đây thiếu provider là
  // trường hợp hợp lệ (test dựng component lẻ), và ngữ cảnh công ty chỉ là thông tin bổ sung.
  const companyContext = useContext(CompanyContext);
  const currentCompanyId = companyContext?.currentCompanyId ?? null;

  const hasPermission = useCallback(
    (code: string, companyId?: number) => {
      const entry = permissions.find((p) => p.permissionCode === code);
      if (!entry) return false;
      if (entry.isGlobal) return true;

      const target = companyId ?? currentCompanyId;
      if (target === null || target === undefined) {
        return entry.companyIds.length > 0;
      }
      return entry.companyIds.includes(target);
    },
    [permissions, currentCompanyId],
  );

  return { permissions, hasPermission };
}

/**
 * AuthProvider bootstraps auth state on mount via silent refresh.
 * All token state is kept in authState module (in-memory).
 * This component holds only the UI-reactive shadow of that state.
 */
export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [mustChangePassword, setMustChangePassword] = useState(false);
  const [user, setUser] = useState<LoginUserInfo | null>(null);
  const [permissions, setPermissions] = useState<CurrentUserPermissionDto[]>([]);
  const [isBootstrapping, setIsBootstrapping] = useState(true);

  /**
   * Apply auth response to both in-memory state and local React state.
   * Never writes access token to localStorage/sessionStorage/cookies.
   */
  const applyAuth = useCallback(
    async (
      accessToken: string,
      mcp: boolean,
      authUser: LoginUserInfo | null,
    ) => {
      // Temporarily set the access token in memory so the permission fetch uses it
      setAuthState(accessToken, mcp, authUser, []);

      let fetchedPermissions: CurrentUserPermissionDto[] = [];
      if (!mcp && authUser) {
        try {
          const permResult = await apiFetchMyPermissions();
          fetchedPermissions = permResult.permissions;
        } catch {
          // Error fetching permissions (401/403) will leave permissions empty
        }
      }

      setAuthState(accessToken, mcp, authUser, fetchedPermissions);
      setIsAuthenticated(true);
      setMustChangePassword(mcp);
      setUser(authUser);
      setPermissions(fetchedPermissions);
    },
    [],
  );

  const refreshPermissions = useCallback(async (companyId?: number) => {
    try {
      const permResult = await apiFetchMyPermissions(companyId);
      setPermissions(permResult.permissions);
      const { accessToken, mustChangePassword, user } = getAuthState();
      setAuthState(accessToken ?? "", mustChangePassword, user, permResult.permissions);
    } catch {
      // Ignore
    }
  }, []);

  const clearAuth = useCallback(() => {
    clearAuthState();
    setIsAuthenticated(false);
    setMustChangePassword(false);
    setUser(null);
    setPermissions([]);
  }, []);

  /**
   * Bootstrap: attempt silent refresh to restore session on page load.
   * If refresh fails (no cookie / expired session), stay unauthenticated.
   */
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const response = await apiRefresh();
        if (!cancelled) {
          await applyAuth(response.accessToken, response.mustChangePassword, response.user);
        }
      } catch {
        if (!cancelled) {
          clearAuth();
        }
      } finally {
        if (!cancelled) {
          setIsBootstrapping(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [applyAuth, clearAuth]);

  /**
   * Setup axios request/response interceptors to inject Bearer token from in-memory state.
   * The interceptors read from authState module (not localStorage/sessionStorage).
   */
  useEffect(() => {
    const reqInterceptor = axiosClient.interceptors.request.use((config) => {
      const { accessToken } = getAuthState();
      if (accessToken) {
        config.headers = config.headers ?? {};
        config.headers['Authorization'] = `Bearer ${accessToken}`;
      }
      return config;
    });

    const resInterceptor = axiosClient.interceptors.response.use(
      (res) => res,
      async (error: unknown) => {
        const err = error as { response?: { status?: number }; config?: Record<string, unknown> & { _retried?: boolean; url?: string; headers?: Record<string, string> } };
        const status = err?.response?.status;
        const originalRequest = err?.config;

        // On 401 from non-loop endpoints, attempt silent refresh once.
        // Exclude the four endpoints that would cause infinite loops:
        //   /auth/login, /auth/refresh, /auth/logout, /auth/change-password
        // /auth/me/* endpoints (companies, permissions) ARE eligible for retry.
        const isLoopEndpoint =
          originalRequest?.url?.includes('/auth/login') ||
          originalRequest?.url?.includes('/auth/refresh') ||
          originalRequest?.url?.includes('/auth/logout') ||
          originalRequest?.url?.includes('/auth/change-password');
        if (
          status === 401 &&
          !originalRequest?._retried &&
          !isLoopEndpoint
        ) {
          if (originalRequest) originalRequest._retried = true;
          try {
            const refreshed = await apiRefresh();
            await applyAuth(
              refreshed.accessToken,
              refreshed.mustChangePassword,
              refreshed.user,
            );
            if (originalRequest?.headers) {
              originalRequest.headers['Authorization'] = `Bearer ${refreshed.accessToken}`;
            }
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            return axiosClient(originalRequest as unknown as any);
          } catch {
            clearAuth();
          }
        }

        return Promise.reject(error);
      },
    );

    return () => {
      axiosClient.interceptors.request.eject(reqInterceptor);
      axiosClient.interceptors.response.eject(resInterceptor);
    };
  }, [applyAuth, clearAuth]);

  const login = useCallback(
    async (username: string, password: string) => {
      const req: LoginRequest = { Username: username, Password: password };
      const response = await apiLogin(req);
      await applyAuth(response.accessToken, response.mustChangePassword, response.user);
    },
    [applyAuth],
  );

  const logout = useCallback(async () => {
    try {
      await apiLogout();
    } finally {
      clearAuth();
    }
  }, [clearAuth]);

  /**
   * Called after successful change-password.
   * Phase G requires fresh login after password change â€” clear auth and redirect to /login.
   */
  const onPasswordChanged = useCallback(() => {
    clearAuth();
  }, [clearAuth]);

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        mustChangePassword,
        user,
        isBootstrapping,
        permissions,
        login,
        logout,
        onPasswordChanged,
        refreshPermissions,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
