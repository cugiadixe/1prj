import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomerDetailPage from './CustomerDetailPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./customersApi', () => ({
  getCustomerById: vi.fn(),
  getCompanyContexts: vi.fn(),
}));

let mockHasPermission = vi.fn();

import { getCustomerById, getCompanyContexts } from './customersApi';
const mockGetCustomer = vi.mocked(getCustomerById);
const mockGetContexts = vi.mocked(getCompanyContexts);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (id = '1') =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/customers/${id}`]}>
        <Routes>
          <Route path="/customers/:customerId" element={<CustomerDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

const mockCustomer = {
  id: 1,
  customerCode: 'C001',
  customerStatus: 'ACTIVE',
  rowVersion: 'AAAA',
  createdAt: '2026-01-01',
  updatedAt: null,
  profile: {
    id: 1,
    fullName: 'Alice Nguyen',
    cccd: '****1234',
    dob: null,
    dobPartial: null,
    dobPrecision: null,
    gender: 'FEMALE',
    permanentAddress: '***',
    cccdIssueDate: null,
    cccdIssuePlace: null,
    taxCode: null,
    phone: '***567',
    contactAddress: null,
    deathDateSolar: null,
    deathDateLunar: null,
    deathPlace: null,
    hometown: null,
    isActive: true,
    rowVersion: 'BBBB',
  },
};

describe('CustomerDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('renders customer detail', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockGetContexts.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-detail-page')).toBeInTheDocument();
      expect(screen.getByText('Alice Nguyen')).toBeInTheDocument();
    });
  });

  it('displays masked values with mask indicator', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockGetContexts.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      const cccd = screen.getByTestId('profile-cccd');
      expect(cccd.textContent).toContain('****1234');
      expect(cccd.textContent).toContain('masked');
    });
  });

  it('shows edit button only with CUSTOMER_MASTER_UPDATE', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockGetContexts.mockResolvedValue([]);
    mockHasPermission.mockImplementation((p: string) => p === 'CUSTOMER_MASTER_UPDATE');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('edit-customer-btn')).toBeInTheDocument();
    });
  });

  it('hides edit button without CUSTOMER_MASTER_UPDATE', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockGetContexts.mockResolvedValue([]);
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('edit-customer-btn')).not.toBeInTheDocument();
    });
  });

  it('shows add context button only with CUSTOMER_CREATE_FINAL', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockGetContexts.mockResolvedValue([]);
    mockHasPermission.mockImplementation((p: string) => p === 'CUSTOMER_CREATE_FINAL');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('add-context-btn')).toBeInTheDocument();
    });
  });

  it('shows no-contexts message when empty', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockGetContexts.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('no-contexts')).toBeInTheDocument();
    });
  });

  it('shows error on fetch failure', async () => {
    mockGetCustomer.mockRejectedValue(new Error('fail'));
    mockGetContexts.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-detail-error')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetCustomer.mockRejectedValue({ response: { status: 403 } });
    mockGetContexts.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
