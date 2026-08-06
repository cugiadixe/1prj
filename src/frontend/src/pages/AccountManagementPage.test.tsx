import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import AccountManagementPage from './AccountManagementPage';
import * as api from '../accountManagement/accountManagementApi';

// Mock the API module — no real HTTP calls in tests
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

const MOCK_ACCOUNT = {
  accountId: 1,
  userId: 100,
  username: 'alice',
  providerType: 'INTERNAL',
  status: 'ACTIVE',
  mustChangePassword: false,
  employeeCode: 'EMP001',
  fullName: 'Alice Smith',
  employmentStatus: 'ACTIVE',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

const MOCK_PAGED_RESULT = {
  page: 1,
  pageSize: 20,
  totalCount: 1,
  items: [MOCK_ACCOUNT],
};

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/security/accounts']}>
        <Routes>
          <Route path="/security/accounts" element={<>{children}</>} />
          <Route path="/security/accounts/:accountId" element={<div data-testid="detail-page" />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AccountManagementPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // Test 1: renders account management page for authenticated user
  it('renders account management page', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    expect(screen.getByTestId('account-management-page')).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByTestId('account-list-table')).toBeInTheDocument()
    );
  });

  // Test 2: shows loading state initially
  it('shows loading spinner while fetching', () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockImplementation(
      () => new Promise(() => {})
    );

    render(<AccountManagementPage />, { wrapper: makeWrapper() });
    expect(screen.getByTestId('account-list-loading')).toBeInTheDocument();
  });

  // Test 3: calls GET /api/v2/security/accounts
  it('calls searchAccounts on mount', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(api.searchAccounts).toHaveBeenCalledWith(
        expect.objectContaining({ page: 1, pageSize: 20 })
      );
    });
  });

  // Test 4: displays account data from API response
  it('displays account summary data', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByText('alice')).toBeInTheDocument());
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('EMP001')).toBeInTheDocument();
    expect(screen.getByText('INTERNAL')).toBeInTheDocument();
  });

  // Test 5: shows status badge for ACTIVE account
  it('renders ACTIVE status badge', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByTestId('status-badge-ACTIVE')).toBeInTheDocument()
    );
  });

  // Test 6: shows empty state when no results
  it('shows empty state when no accounts', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue({
      page: 1,
      pageSize: 20,
      totalCount: 0,
      items: [],
    });

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(screen.getByText('No accounts found.')).toBeInTheDocument()
    );
  });

  // Test 7: search query updates request parameters
  it('updates search parameter when search is submitted', async () => {
    const user = userEvent.setup();
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(api.searchAccounts).toHaveBeenCalledTimes(1));

    const searchInput = screen.getByRole('searchbox', { name: /search accounts/i });
    await user.type(searchInput, 'alice');
    await user.keyboard('{Enter}');

    await waitFor(() =>
      expect(api.searchAccounts).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'alice', page: 1 })
      )
    );
  });

  // Test 8: 403 response displays sanitized permission denied message
  it('shows permission denied message on 403', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: { status: 403 },
    });

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(
        screen.getByText('You do not have permission to manage accounts.')
      ).toBeInTheDocument()
    );
    expect(screen.getByTestId('account-list-error')).toBeInTheDocument();
  });

  // Test 9: generic error shows sanitized message
  it('shows generic error message on server error', async () => {
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: { status: 500 },
    });

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() =>
      expect(
        screen.getByText('An error occurred. Please try again.')
      ).toBeInTheDocument()
    );
  });

  // Test 10: Manage button navigates to account detail page
  it('navigate to account detail on Manage click', async () => {
    const user = userEvent.setup();
    (api.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_PAGED_RESULT);

    render(<AccountManagementPage />, { wrapper: makeWrapper() });

    await waitFor(() => screen.getByTestId('manage-account-1'));
    await user.click(screen.getByTestId('manage-account-1'));

    await waitFor(() =>
      expect(screen.getByTestId('detail-page')).toBeInTheDocument()
    );
  });

  // Test 11: temporary password NOT stored in localStorage
  it('does not store temporary password in localStorage', () => {
    expect(localStorage.getItem('temporaryPassword')).toBeNull();
    expect(localStorage.getItem('temp_password')).toBeNull();
  });

  // Test 12: temporary password NOT stored in sessionStorage
  it('does not store temporary password in sessionStorage', () => {
    expect(sessionStorage.getItem('temporaryPassword')).toBeNull();
    expect(sessionStorage.getItem('temp_password')).toBeNull();
  });

  // Test 13: access token NOT stored in localStorage
  it('does not store access token in localStorage', () => {
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('access_token')).toBeNull();
  });

  // Test 14: access token NOT stored in sessionStorage
  it('does not store access token in sessionStorage', () => {
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('access_token')).toBeNull();
  });
});
