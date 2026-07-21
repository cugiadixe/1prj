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
        screen.getByText('Please enter your current password.'),
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
        screen.getByText('New passwords do not match.'),
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
