import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomerEditPage from './CustomerEditPage';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
  }),
}));

vi.mock('./customersApi', () => ({
  getCustomerById: vi.fn(),
  updateCustomer: vi.fn(),
}));

import { getCustomerById, updateCustomer } from './customersApi';
const mockGetCustomer = vi.mocked(getCustomerById);
const mockUpdate = vi.mocked(updateCustomer);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (id = '1') =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/customers/${id}/edit`]}>
        <Routes>
          <Route path="/customers/:customerId/edit" element={<CustomerEditPage />} />
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
    fullName: 'Alice',
    cccd: '123',
    dob: null,
    dobPartial: null,
    dobPrecision: null,
    gender: 'FEMALE',
    permanentAddress: null,
    cccdIssueDate: null,
    cccdIssuePlace: null,
    taxCode: null,
    phone: '555',
    contactAddress: null,
    deathDateSolar: null,
    deathDateLunar: null,
    deathPlace: null,
    hometown: null,
    isActive: true,
    rowVersion: 'BBBB',
  },
};

describe('CustomerEditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders edit form pre-filled', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-edit-page')).toBeInTheDocument();
      expect(screen.getByTestId('input-fullName')).toHaveValue('Alice');
    });
  });

  it('has required reason field', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('input-reason')).toBeInTheDocument();
    });
  });

  it('shows concurrency error with refresh button on 409', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockUpdate.mockRejectedValue({
      response: {
        status: 409,
        data: { extensions: { errorCode: 'CUS_INVALID_ROW_VERSION' } },
      },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-edit-form')).toBeInTheDocument();
    });

    fireEvent.change(screen.getByTestId('input-reason'), { target: { value: 'test reason' } });
    fireEvent.click(screen.getByTestId('submit-update'));

    await waitFor(() => {
      expect(screen.getByTestId('edit-error')).toBeInTheDocument();
      expect(screen.getByTestId('refresh-btn')).toBeInTheDocument();
    });
  });

  it('navigates to detail on successful update', async () => {
    mockGetCustomer.mockResolvedValue(mockCustomer);
    mockUpdate.mockResolvedValue({ ...mockCustomer, rowVersion: 'CCCC' });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-edit-form')).toBeInTheDocument();
    });

    fireEvent.change(screen.getByTestId('input-reason'), { target: { value: 'update reason' } });
    fireEvent.click(screen.getByTestId('submit-update'));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/customers/1');
    });
  });

  it('shows permission denied on 403 fetch', async () => {
    mockGetCustomer.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows generic error on fetch failure', async () => {
    mockGetCustomer.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('customer-edit-fetch-error')).toBeInTheDocument();
    });
  });
});
