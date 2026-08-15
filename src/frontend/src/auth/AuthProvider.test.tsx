import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider, useAuth, usePermissions } from './AuthProvider';
import { clearAuthState } from './authState';
import * as authApi from './authApi';

/**
 * Test double: wraps AuthProvider in necessary providers.
 */
const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <AuthProvider>{children}</AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
};

/**
 * Helper component to expose auth context values for assertions.
 */
const AuthInspector: React.FC = () => {
  const { isAuthenticated, mustChangePassword, user, isBootstrapping } = useAuth();
  return (
    <div>
      <span data-testid="isAuthenticated">{String(isAuthenticated)}</span>
      <span data-testid="mustChangePassword">{String(mustChangePassword)}</span>
      <span data-testid="username">{user?.username ?? ''}</span>
      <span data-testid="isBootstrapping">{String(isBootstrapping)}</span>
    </div>
  );
};

const mockLoginResponse = (mustChangePassword = false) => ({
  accessToken: 'test-access-token',
  tokenType: 'Bearer',
  expiresIn: 900,
  expiresAtUtc: new Date(Date.now() + 900000).toISOString(),
  user: { userId: 1, username: 'testuser', displayName: null },
  mustChangePassword,
});

describe('AuthProvider', () => {
  beforeEach(() => {
    clearAuthState();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('bootstraps unauthenticated when refresh fails', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));

    render(<AuthInspector />, { wrapper: TestWrapper });

    await waitFor(() => {
      expect(screen.getByTestId('isBootstrapping').textContent).toBe('false');
    });
    expect(screen.getByTestId('isAuthenticated').textContent).toBe('false');
  });

  it('bootstraps authenticated when refresh succeeds', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));

    render(<AuthInspector />, { wrapper: TestWrapper });

    await waitFor(() => {
      expect(screen.getByTestId('isAuthenticated').textContent).toBe('true');
    });
    expect(screen.getByTestId('username').textContent).toBe('testuser');
  });

  it('login success stores access token in memory only', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));
    vi.spyOn(authApi, 'apiLogin').mockResolvedValue(mockLoginResponse(false));

    const LoginTrigger: React.FC = () => {
      const { login, isAuthenticated } = useAuth();
      return (
        <div>
          <button data-testid="do-login" onClick={() => login('u', 'p')}>
            Login
          </button>
          <span data-testid="isAuthenticated">{String(isAuthenticated)}</span>
        </div>
      );
    };

    render(<LoginTrigger />, { wrapper: TestWrapper });
    await waitFor(() => expect(screen.getByTestId('isAuthenticated').textContent).toBe('false'));

    await userEvent.click(screen.getByTestId('do-login'));
    await waitFor(() =>
      expect(screen.getByTestId('isAuthenticated').textContent).toBe('true'),
    );

    // Access token must NOT be in localStorage or sessionStorage
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });

  it('login success with mustChangePassword=true sets mustChangePassword', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));
    vi.spyOn(authApi, 'apiLogin').mockResolvedValue(mockLoginResponse(true));

    const MustChangeTrigger: React.FC = () => {
      const { login, mustChangePassword } = useAuth();
      return (
        <div>
          <button data-testid="do-login" onClick={() => login('u', 'p')}>
            Login
          </button>
          <span data-testid="mustChangePassword">{String(mustChangePassword)}</span>
        </div>
      );
    };

    render(<MustChangeTrigger />, { wrapper: TestWrapper });
    await waitFor(() => expect(screen.getByTestId('mustChangePassword').textContent).toBe('false'));

    await userEvent.click(screen.getByTestId('do-login'));
    await waitFor(() =>
      expect(screen.getByTestId('mustChangePassword').textContent).toBe('true'),
    );
  });

  it('logout clears auth state', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
    vi.spyOn(authApi, 'apiLogout').mockResolvedValue(undefined);

    const LogoutTrigger: React.FC = () => {
      const { logout, isAuthenticated } = useAuth();
      return (
        <div>
          <button data-testid="do-logout" onClick={() => logout()}>
            Logout
          </button>
          <span data-testid="isAuthenticated">{String(isAuthenticated)}</span>
        </div>
      );
    };

    render(<LogoutTrigger />, { wrapper: TestWrapper });
    await waitFor(() => expect(screen.getByTestId('isAuthenticated').textContent).toBe('true'));

    await userEvent.click(screen.getByTestId('do-logout'));
    await waitFor(() =>
      expect(screen.getByTestId('isAuthenticated').textContent).toBe('false'),
    );
  });

  it('onPasswordChanged clears auth state', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(true));

    const PwChangeTrigger: React.FC = () => {
      const { onPasswordChanged, isAuthenticated } = useAuth();
      return (
        <div>
          <button data-testid="do-pw-change" onClick={onPasswordChanged}>
            PwChanged
          </button>
          <span data-testid="isAuthenticated">{String(isAuthenticated)}</span>
        </div>
      );
    };

    render(<PwChangeTrigger />, { wrapper: TestWrapper });
    await waitFor(() => expect(screen.getByTestId('isAuthenticated').textContent).toBe('true'));

    await userEvent.click(screen.getByTestId('do-pw-change'));
    await waitFor(() =>
      expect(screen.getByTestId('isAuthenticated').textContent).toBe('false'),
    );
  });

  describe('usePermissions', () => {
    it('fetches permissions on login and exposes them via hasPermission', async () => {
      vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));
      vi.spyOn(authApi, 'apiLogin').mockResolvedValue(mockLoginResponse(false));
      vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({
        permissions: [
          { permissionCode: 'SECURITY_ACCOUNT_MANAGE', isGlobal: true, companyIds: [] },
          { permissionCode: 'TEST_PERM', isGlobal: false, companyIds: [10] }
        ]
      });

      const PermInspector: React.FC = () => {
        const { login } = useAuth();
        const { permissions, hasPermission } = usePermissions();
        return (
          <div>
            <button data-testid="do-login" onClick={() => login('u', 'p')}>Login</button>
            <span data-testid="perm-length">{permissions.length}</span>
            <span data-testid="has-global">{String(hasPermission('SECURITY_ACCOUNT_MANAGE'))}</span>
            <span data-testid="has-company-10">{String(hasPermission('TEST_PERM', 10))}</span>
            <span data-testid="has-company-20">{String(hasPermission('TEST_PERM', 20))}</span>
            <span data-testid="has-any">{String(hasPermission('SECURITY_ACCOUNT_MANAGE'))}</span>
          </div>
        );
      };

      render(<PermInspector />, { wrapper: TestWrapper });
      await userEvent.click(screen.getByTestId('do-login'));
      await waitFor(() => expect(screen.getByTestId('perm-length').textContent).toBe('2'));

      expect(screen.getByTestId('has-global').textContent).toBe('true');
      expect(screen.getByTestId('has-company-10').textContent).toBe('true');
      expect(screen.getByTestId('has-company-20').textContent).toBe('false');
      expect(screen.getByTestId('has-any').textContent).toBe('true');
    });
  });
});
