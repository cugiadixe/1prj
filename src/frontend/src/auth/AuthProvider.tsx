import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from 'react';
import axiosClient from '../api/axiosClient';
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
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export function usePermissions() {
  const { permissions } = useAuth();
  const hasPermission = useCallback(
    (code: string, scope?: string, companyId?: number) => {
      return permissions.some(
        (p) =>
          p.permissionCode === code &&
          (!scope || p.scope === scope) &&
          (companyId === undefined || p.companyId === companyId),
      );
    },
    [permissions],
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

        // On 401 from non-auth endpoints, attempt silent refresh once
        if (
          status === 401 &&
          !originalRequest?._retried &&
          !originalRequest?.url?.includes('/auth/')
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
   * Phase G requires fresh login after password change — clear auth and redirect to /login.
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
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
