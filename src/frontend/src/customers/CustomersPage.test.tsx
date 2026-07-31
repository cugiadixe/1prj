import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomersPage from './CustomersPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./customersApi', () => ({
  searchCustomers: vi.fn(),
}));

let mockHasPermission = vi.fn();

import { searchCustomers } from './customersApi';
const mockSearchCustomers = vi.mocked(searchCustomers);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <CustomersPage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('CustomersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('renders the customers page', async () => {
    mockSearchCustomers.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customers-page')).toBeInTheDocument();
    });
  });

  it('shows empty state when no customers', async () => {
    mockSearchCustomers.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-list-empty')).toBeInTheDocument();
    });
  });

  it('renders customer rows from API', async () => {
    mockSearchCustomers.mockResolvedValue({
      items: [
        { id: 1, customerCode: 'C001', fullName: 'Alice', cccd: '123', phone: '555', customerStatus: 'ACTIVE', createdAt: '2026-01-01' },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('C001')).toBeInTheDocument();
      expect(screen.getByText('Alice')).toBeInTheDocument();
    });
  });

  it('shows create button only with CUSTOMER_CREATE_FINAL', async () => {
    mockSearchCustomers.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    mockHasPermission.mockImplementation((p: string) => p === 'CUSTOMER_CREATE_FINAL');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('create-customer-btn')).toBeInTheDocument();
    });
  });

  it('hides create button without CUSTOMER_CREATE_FINAL', async () => {
    mockSearchCustomers.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('create-customer-btn')).not.toBeInTheDocument();
    });
  });

  it('shows error state on API failure', async () => {
    mockSearchCustomers.mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-list-error')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockSearchCustomers.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
