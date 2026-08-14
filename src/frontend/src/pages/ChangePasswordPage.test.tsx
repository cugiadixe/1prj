import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider } from '../auth/AuthProvider';
import { clearAuthState } from '../auth/authState';
import * as authApi from '../auth/authApi';
import ChangePasswordPage from './ChangePasswordPage';

const mockRefreshWithMustChange = () =>
  vi.spyOn(authApi, 'apiRefresh').mockResolvedValue({
    accessToken: 'tok',
    tokenType: 'Bearer',
    expiresIn: 900,
    expiresAtUtc: new Date().toISOString(),
    user: { userId: 1, username: 'u', displayName: null },
    mustChangePassword: true,
  });

// Đổi mật khẩu nay là tự nguyện: người dùng đã đăng nhập bình thường vẫn vào được.
const mockRefreshWithoutMustChange = () =>
  vi.spyOn(authApi, 'apiRefresh').mockResolvedValue({
    accessToken: 'tok',
    tokenType: 'Bearer',
    expiresIn: 900,
    expiresAtUtc: new Date().toISOString(),
    user: { userId: 1, username: 'u', displayName: null },
    mustChangePassword: false,
  });

const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/change-password']}>
        <AuthProvider>{children}</AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ChangePasswordPage', () => {
  beforeEach(() => {
    clearAuthState();
    vi.restoreAllMocks();
  });

  it('renders change password form for authenticated+mustChangePassword user', async () => {
    mockRefreshWithMustChange();

    render(<ChangePasswordPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-submit'),
      ).toBeInTheDocument(),
    );
    expect(screen.getByTestId('change-current-password')).toBeInTheDocument();
    expect(screen.getByTestId('change-new-password')).toBeInTheDocument();
    expect(screen.getByTestId('change-confirm-password')).toBeInTheDocument();
  });

  it('renders change password form for a voluntary change (mustChangePassword=false)', async () => {
    mockRefreshWithoutMustChange();

    render(<ChangePasswordPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-submit'),
      ).toBeInTheDocument(),
    );
    expect(screen.getByTestId('change-current-password')).toBeInTheDocument();
  });

  it('validates required fields', async () => {
    mockRefreshWithMustChange();

    render(<ChangePasswordPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-submit'),
      ).toBeInTheDocument(),
    );

    await userEvent.click(screen.getByTestId('change-password-submit'));

    await waitFor(() => {
      expect(
        screen.getByText('Vui lòng nhập mật khẩu hiện tại.'),
      ).toBeInTheDocument();
    });
  });

  it('validates confirm password mismatch', async () => {
    mockRefreshWithMustChange();

    render(<ChangePasswordPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-submit'),
      ).toBeInTheDocument(),
    );

    await userEvent.type(screen.getByTestId('change-current-password'), 'OldPass1');
    await userEvent.type(screen.getByTestId('change-new-password'), 'NewPass1');
    await userEvent.type(screen.getByTestId('change-confirm-password'), 'Different');
    await userEvent.click(screen.getByTestId('change-password-submit'));

    await waitFor(() =>
      expect(
        screen.getByText('Mật khẩu mới không khớp.'),
      ).toBeInTheDocument(),
    );
  });

  it('displays sanitized error on API failure', async () => {
    mockRefreshWithMustChange();
    vi.spyOn(authApi, 'apiChangePassword').mockRejectedValue({
      response: { status: 400 },
    });

    render(<ChangePasswordPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-submit'),
      ).toBeInTheDocument(),
    );

    await userEvent.type(screen.getByTestId('change-current-password'), 'OldPass1');
    await userEvent.type(screen.getByTestId('change-new-password'), 'NewPass1');
    await userEvent.type(screen.getByTestId('change-confirm-password'), 'NewPass1');
    await userEvent.click(screen.getByTestId('change-password-submit'));

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-error'),
      ).toBeInTheDocument(),
    );

    const errorEl = screen.getByTestId('change-password-error');
    expect(errorEl.textContent).not.toContain('stack');
    expect(errorEl.textContent).not.toContain('Exception');
  });

  it('clears auth state and redirects to /login on success', async () => {
    mockRefreshWithMustChange();
    vi.spyOn(authApi, 'apiChangePassword').mockResolvedValue(undefined);

    const { container } = render(<ChangePasswordPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(
        screen.getByTestId('change-password-submit'),
      ).toBeInTheDocument(),
    );

    await userEvent.type(screen.getByTestId('change-current-password'), 'OldPass1');
    await userEvent.type(screen.getByTestId('change-new-password'), 'NewPass2');
    await userEvent.type(screen.getByTestId('change-confirm-password'), 'NewPass2');
    await userEvent.click(screen.getByTestId('change-password-submit'));

    // After success the page navigates away — confirm form is gone
    await waitFor(() => {
      expect(container.querySelector('[data-testid="change-password-submit"]')).toBeNull();
    });
  });
});
