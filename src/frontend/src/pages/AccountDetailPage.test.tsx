import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import AccountDetailPage from './AccountDetailPage';
import * as api from '../accountManagement/accountManagementApi';

// Mock the API module
vi.mock('../accountManagement/accountManagementApi', () => ({
  searchAccounts: vi.fn(),
  getAccountsByUserId: vi.fn(),
  getAccountDetail: vi.fn(),
  activateAccount: vi.fn(),
  disableAccount: vi.fn(),
  lockAccount: vi.fn(),
  unlockAccount: vi.fn(),
  resetPassword: vi.fn(),
  revokeSessions: vi.fn(),
}));

const MOCK_ACTIVE_ACCOUNT = {
  id: 42,
  userId: 100,
  providerType: 'INTERNAL',
  username: 'alice',
  status: 'ACTIVE',
  isInternalProvider: true,
  failedAttemptCount: 0,
  isManualLock: false,
  lockoutEnd: null,
  mustChangePassword: false,
  temporaryPasswordExpiresAt: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

const MOCK_LOCKED_ACCOUNT = { ...MOCK_ACTIVE_ACCOUNT, status: 'LOCKED' };
const MOCK_DISABLED_ACCOUNT = { ...MOCK_ACTIVE_ACCOUNT, status: 'DISABLED' };
const MOCK_MCP_ACCOUNT = { ...MOCK_ACTIVE_ACCOUNT, mustChangePassword: true };

function makeWrapper(accountId = '42') {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/security/accounts/${accountId}`]}>
        <Routes>
          <Route path="/security/accounts" element={<div data-testid="list-page" />} />
          <Route path="/security/accounts/:accountId" element={<>{children}</>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AccountDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // Test 1: Renders detail page for authenticated user
  it('renders account detail page', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('account-detail-page')).toBeInTheDocument()
    );
    expect(screen.getByText('alice')).toBeInTheDocument();
  });

  // Test 2: Shows loading state
  it('shows loading spinner', () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockImplementation(
      () => new Promise(() => {})
    );

    render(<AccountDetailPage />, { wrapper: makeWrapper() });
    expect(screen.getByTestId('account-detail-loading')).toBeInTheDocument();
  });

  // Test 3: Shows ACTIVE status badge
  it('shows ACTIVE status badge with correct color', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('account-status-badge')).toBeInTheDocument()
    );
    expect(screen.getByTestId('account-status-badge')).toHaveTextContent('ACTIVE');
  });

  // Test 4: Shows MustChangePassword warning
  it('shows mustChangePassword warning banner', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_MCP_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('must-change-password-warning')).toBeInTheDocument()
    );
  });

  // Test 5: Activate button visible for DISABLED account
  it('shows activate button for DISABLED account', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_DISABLED_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('activate-button')).toBeInTheDocument()
    );
    expect(screen.queryByTestId('disable-button')).toBeNull();
    expect(screen.queryByTestId('lock-button')).toBeNull();
  });

  // Test 6: Disable button visible for ACTIVE account
  it('shows disable and lock buttons for ACTIVE account', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('disable-button')).toBeInTheDocument()
    );
    expect(screen.getByTestId('lock-button')).toBeInTheDocument();
    expect(screen.queryByTestId('activate-button')).toBeNull();
  });

  // Test 7: Confirmation modal appears for disable action
  it('opens confirmation modal on disable click', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('disable-button'));
    await user.click(screen.getByTestId('disable-button'));

    // Modal content is rendered into a portal — check by modal title text
    await waitFor(() =>
      expect(screen.getByText('Disable Account')).toBeInTheDocument()
    );
  });

  // Test 8: Disable action requires reason (client-side validation)
  it('shows validation error when disabling without reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('disable-button'));
    await user.click(screen.getByTestId('disable-button'));

    // Modal should be open — find Confirm button within modal context
    await waitFor(() => screen.getByText('Disable Account'));
    const confirmBtn = screen.getByRole('button', { name: /^confirm$/i });
    await user.click(confirmBtn);

    expect(screen.getByText('A reason is required.')).toBeInTheDocument();
    expect(api.disableAccount).not.toHaveBeenCalled();
  });

  // Test 9: Disable calls correct endpoint with reason
  it('calls disableAccount with reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.disableAccount as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('disable-button'));
    await user.click(screen.getByTestId('disable-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'Policy violation');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.disableAccount).toHaveBeenCalledWith(42, 'Policy violation')
    );
  });

  // Test 10: Lock action requires reason
  it('shows validation error when locking without reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('lock-button'));
    await user.click(screen.getByTestId('lock-button'));

    await waitFor(() => screen.getByText('Lock Account'));
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    expect(screen.getByText('A reason is required.')).toBeInTheDocument();
    expect(api.lockAccount).not.toHaveBeenCalled();
  });

  // Test 11: Lock calls correct endpoint with reason
  it('calls lockAccount with reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.lockAccount as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('lock-button'));
    await user.click(screen.getByTestId('lock-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'Suspicious activity');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.lockAccount).toHaveBeenCalledWith(42, 'Suspicious activity')
    );
  });

  // Test 12: Unlock calls correct endpoint (no reason required)
  it('calls unlockAccount on confirm', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_LOCKED_ACCOUNT);
    (api.unlockAccount as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('unlock-button'));
    await user.click(screen.getByTestId('unlock-button'));
    await waitFor(() => screen.getByText('Unlock Account'));
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.unlockAccount).toHaveBeenCalledWith(42)
    );
  });

  // Test 13: Activate calls correct endpoint (no reason required)
  it('calls activateAccount on confirm', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_DISABLED_ACCOUNT);
    (api.activateAccount as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('activate-button'));
    await user.click(screen.getByTestId('activate-button'));
    await waitFor(() => screen.getByText('Activate Account'));
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.activateAccount).toHaveBeenCalledWith(42)
    );
  });

  // Test 14: Reset password requires reason
  it('shows validation error when resetting password without reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));

    // Wait for the reason-input to appear (modal is open)
    await waitFor(() => screen.getByTestId('reason-input'));
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    expect(screen.getByText('A reason is required.')).toBeInTheDocument();
    expect(api.resetPassword).not.toHaveBeenCalled();
  });

  // Test 15: Reset password calls correct endpoint with reason
  it('calls resetPassword with reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.resetPassword as ReturnType<typeof vi.fn>).mockResolvedValue({
      temporaryPassword: 'TempPass123!',
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'User request');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.resetPassword).toHaveBeenCalledWith(42, 'User request')
    );
  });

  // Test 16: Reset password displays temporary password once
  it('displays temporary password in modal after reset', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.resetPassword as ReturnType<typeof vi.fn>).mockResolvedValue({
      temporaryPassword: 'TempPass123!',
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'User request');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(screen.getByTestId('temp-password-display')).toBeInTheDocument()
    );
    expect(screen.getByTestId('temp-password-display')).toHaveTextContent('TempPass123!');
  });

  // Test 17: Temporary password can be dismissed
  it('dismisses temporary password modal on close', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.resetPassword as ReturnType<typeof vi.fn>).mockResolvedValue({
      temporaryPassword: 'TempPass123!',
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));
    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'User request');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() => screen.getByTestId('dismiss-temp-password-button'));
    await user.click(screen.getByTestId('dismiss-temp-password-button'));

    await waitFor(() =>
      expect(screen.queryByTestId('temp-password-display')).toBeNull()
    );
  });

  // Test 18: Temporary password not stored in localStorage
  it('does not store temporary password in localStorage', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.resetPassword as ReturnType<typeof vi.fn>).mockResolvedValue({
      temporaryPassword: 'TempPass123!',
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));
    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'User request');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() => screen.getByTestId('temp-password-display'));

    // Verify temporary password is not in localStorage
    expect(localStorage.getItem('temporaryPassword')).toBeNull();
    expect(localStorage.getItem('temp_password')).toBeNull();
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i) ?? '';
      const val = localStorage.getItem(key) ?? '';
      expect(val).not.toContain('TempPass123!');
    }
  });

  // Test 19: Temporary password not stored in sessionStorage
  it('does not store temporary password in sessionStorage', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.resetPassword as ReturnType<typeof vi.fn>).mockResolvedValue({
      temporaryPassword: 'TempPass123!',
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));
    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'User request');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() => screen.getByTestId('temp-password-display'));

    expect(sessionStorage.getItem('temporaryPassword')).toBeNull();
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i) ?? '';
      const val = sessionStorage.getItem(key) ?? '';
      expect(val).not.toContain('TempPass123!');
    }
  });

  // Test 20: Temporary password is not console logged
  it('does not log temporary password to console', async () => {
    const user = userEvent.setup();
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
    const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.resetPassword as ReturnType<typeof vi.fn>).mockResolvedValue({
      temporaryPassword: 'TempPass123!',
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'User request');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() => screen.getByTestId('temp-password-display'));

    // Verify temp password was never logged
    const allLogCalls = [
      ...consoleSpy.mock.calls,
      ...consoleWarnSpy.mock.calls,
      ...consoleErrorSpy.mock.calls,
    ];
    for (const call of allLogCalls) {
      const logString = JSON.stringify(call);
      expect(logString).not.toContain('TempPass123!');
    }

    consoleSpy.mockRestore();
    consoleWarnSpy.mockRestore();
    consoleErrorSpy.mockRestore();
  });

  // Test 21: Revoke sessions requires reason
  it('shows validation error when revoking sessions without reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('revoke-sessions-button'));
    await user.click(screen.getByTestId('revoke-sessions-button'));

    await waitFor(() => screen.getByText('Revoke All Sessions'));
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    expect(screen.getByText('A reason is required.')).toBeInTheDocument();
    expect(api.revokeSessions).not.toHaveBeenCalled();
  });

  // Test 22: Revoke sessions calls correct endpoint with reason
  it('calls revokeSessions with reason', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.revokeSessions as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('revoke-sessions-button'));
    await user.click(screen.getByTestId('revoke-sessions-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'Suspicious login');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.revokeSessions).toHaveBeenCalledWith(42, 'Suspicious login')
    );
  });

  // Test 23: 403 response shows sanitized permission denied
  it('shows permission denied message on 403', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: { status: 403 },
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('account-detail-permission-denied')).toBeInTheDocument()
    );
    expect(
      screen.getByText('You do not have permission to manage accounts.')
    ).toBeInTheDocument();
  });

  // Test 24: 404 response shows account not found
  it('shows account not found message on 404', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: {
        status: 404,
        data: { extensions: { errorCode: 'AUTH_ACCOUNT_NOT_FOUND' } },
      },
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('account-detail-not-found')).toBeInTheDocument()
    );
  });

  // Test 25: Sanitized error shown for action failures
  it('shows sanitized error message for failed action', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.lockAccount as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: {
        status: 409,
        data: {
          extensions: { errorCode: 'AUTH_ACCOUNT_STATE_CONFLICT' },
        },
      },
    });

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('lock-button'));
    await user.click(screen.getByTestId('lock-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    await user.type(screen.getByTestId('reason-input'), 'Test reason');
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(screen.getByTestId('action-error-message')).toBeInTheDocument()
    );
    expect(screen.getByText(
      'This action cannot be performed on the account in its current state.'
    )).toBeInTheDocument();
  });

  // Test 26: Back navigation goes to account list
  it('back button navigates to account list', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('back-to-list-button'));
    await user.click(screen.getByTestId('back-to-list-button'));

    await waitFor(() =>
      expect(screen.getByTestId('list-page')).toBeInTheDocument()
    );
  });

  // Test 27: Access token not in localStorage
  it('does not store access token in localStorage', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('account-detail-page'));

    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('access_token')).toBeNull();
  });

  // Test 28: Access token not in sessionStorage
  it('does not store access token in sessionStorage', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('account-detail-page'));

    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('access_token')).toBeNull();
  });

  // Test 29: Confirmation modal for sensitive actions
  it('shows confirmation modal for revoke-sessions', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('revoke-sessions-button'));
    await user.click(screen.getByTestId('revoke-sessions-button'));

    await waitFor(() =>
      expect(screen.getByText(/revoke all active sessions/i)).toBeInTheDocument()
    );
  });

  // Test 30: Confirmation modal for reset-password shows warning text
  it('confirmation modal for reset-password shows warning text', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('reset-password-button'));
    await user.click(screen.getByTestId('reset-password-button'));

    await waitFor(() =>
      expect(screen.getByText(/generate a new temporary password/i)).toBeInTheDocument()
    );
  });

  // Test 31: Action refetches account data after success
  it('refetches account detail after successful activate', async () => {
    const user = userEvent.setup();
    (api.getAccountDetail as ReturnType<typeof vi.fn>)
      .mockResolvedValueOnce(MOCK_DISABLED_ACCOUNT)
      .mockResolvedValueOnce({ ...MOCK_DISABLED_ACCOUNT, status: 'ACTIVE' });
    (api.activateAccount as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('activate-button'));
    await user.click(screen.getByTestId('activate-button'));
    await waitFor(() => screen.getByText('Activate Account'));
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() => {
      expect(api.getAccountDetail).toHaveBeenCalledTimes(2);
    });
  });

  // Test 32: Account detail page renders all AccountDetailDto fields
  // Note: Ant Design Descriptions.Item doesn't propagate data-testid to DOM;
  // assert by label text and value content instead.
  it('renders all account detail fields', async () => {
    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('account-detail-page'));
    // Descriptions labels
    expect(screen.getByText('Account ID')).toBeInTheDocument();
    expect(screen.getByText('User ID')).toBeInTheDocument();
    expect(screen.getByText('Username')).toBeInTheDocument();
    expect(screen.getByText('Provider Type')).toBeInTheDocument();
    expect(screen.getByText('Failed Attempts')).toBeInTheDocument();
    expect(screen.getByText('Manual Lock')).toBeInTheDocument();
    expect(screen.getByText('Created At')).toBeInTheDocument();
    expect(screen.getByText('Updated At')).toBeInTheDocument();
    // Values from mock data
    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('100')).toBeInTheDocument();
    expect(screen.getByText('alice')).toBeInTheDocument();
    expect(screen.getByText('INTERNAL')).toBeInTheDocument();
  });

  // Test 33: No console logging of secrets during full action flow
  it('does not log any secrets during full revoke-sessions flow', async () => {
    const user = userEvent.setup();
    const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

    (api.getAccountDetail as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACTIVE_ACCOUNT);
    (api.revokeSessions as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);

    render(<AccountDetailPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('revoke-sessions-button'));
    await user.click(screen.getByTestId('revoke-sessions-button'));

    await waitFor(() => screen.getByTestId('reason-input'));
    const reasonText = 'Routine security revocation';
    await user.type(screen.getByTestId('reason-input'), reasonText);
    await user.click(screen.getByRole('button', { name: /^confirm$/i }));

    await waitFor(() =>
      expect(api.revokeSessions).toHaveBeenCalled()
    );

    // Reason text should not be leaked to console
    for (const call of consoleSpy.mock.calls) {
      expect(JSON.stringify(call)).not.toContain('Routine security revocation');
    }

    consoleSpy.mockRestore();
  });
});

// ── Route guard regression tests ─────────────────────────────────────────────
// These verify that existing ProtectedRoute behavior is not broken.

import { AuthProvider } from '../auth/AuthProvider';
import * as authApi from '../auth/authApi';
import ProtectedRoute from '../components/ProtectedRoute';

describe('AccountDetailPage route guard regression', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('unauthenticated user cannot access account management route', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));

    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    render(
      <QueryClientProvider client={qc}>
        <MemoryRouter initialEntries={['/security/accounts/42']}>
          <AuthProvider>
            <Routes>
              <Route path="/login" element={<div data-testid="login-page" />} />
              <Route
                element={
                  <ProtectedRoute>
                    <div />
                  </ProtectedRoute>
                }
              >
                <Route
                  path="/security/accounts/:accountId"
                  element={<div data-testid="protected-account-detail" />}
                />
              </Route>
            </Routes>
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() =>
      expect(screen.queryByTestId('protected-account-detail')).toBeNull()
    );
  });

  it('mustChangePassword user cannot access account management route', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue({
      accessToken: 'tok',
      tokenType: 'Bearer',
      expiresIn: 900,
      expiresAtUtc: new Date().toISOString(),
      user: { userId: 1, username: 'u', displayName: null },
      mustChangePassword: true,
    });

    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    render(
      <QueryClientProvider client={qc}>
        <MemoryRouter initialEntries={['/security/accounts/42']}>
          <AuthProvider>
            <Routes>
              <Route path="/change-password" element={<div data-testid="change-password-page" />} />
              <Route
                element={
                  <ProtectedRoute>
                    <div />
                  </ProtectedRoute>
                }
              >
                <Route
                  path="/security/accounts/:accountId"
                  element={<div data-testid="protected-account-detail" />}
                />
              </Route>
            </Routes>
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() =>
      expect(screen.queryByTestId('protected-account-detail')).toBeNull()
    );
    await waitFor(() =>
      expect(screen.getByTestId('change-password-page')).toBeInTheDocument()
    );
  });
});
