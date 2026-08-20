
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerMasterChangeRequestDetailPage from './CustomerMasterChangeRequestDetailPage';
import { getCustomerMasterChangeRequestById } from './customerMasterChangeApi';

vi.mock('./customerMasterChangeApi', () => ({
  getCustomerMasterChangeRequestById: vi.fn(),
}));

describe('CustomerMasterChangeRequestDetailPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.resetAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const renderComponent = () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/customers/change-requests/456']}>
          <Routes>
            <Route
              path="/customers/change-requests/:id"
              element={<CustomerMasterChangeRequestDetailPage />}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders loading state initially', () => {
    vi.mocked(getCustomerMasterChangeRequestById).mockImplementation(
      () => new Promise(() => {})
    );
    renderComponent();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('renders error state safely', async () => {
    vi.mocked(getCustomerMasterChangeRequestById).mockRejectedValueOnce(
      new Error('Failed to fetch detail')
    );
    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('error-alert')).toHaveTextContent('Đã xảy ra lỗi. Vui lòng thử lại.');
    });
  });

  it('renders detail page safely without raw JSON', async () => {
    vi.mocked(getCustomerMasterChangeRequestById).mockResolvedValueOnce({
      id: 456,
      processCode: 'CUSTOMER_MASTER_CHANGE',
      requesterId: 1,
      companyId: null,
      requestStatus: 'Draft',
      workflowInstanceId: 999,
      targetCustomerId: 123,
      targetCustomerCode: 'KH0000123',
      targetCustomerName: 'Nguyễn Văn A',
      targetRowVersion: 'v1',
      createdAt: '2023-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'v2',
      payload: {
        targetCustomerId: 123,
        targetRowVersion: 'v1',
        reason: 'Fix name',
        fullName: 'Correct Name',
        cccd: null,
        dob: null,
        dobPartial: null,
        dobPrecision: null,
        gender: null,
        permanentAddress: null,
        cccdIssueDate: null,
        cccdIssuePlace: null,
        taxCode: null,
        phone: null,
        contactAddress: null,
        deathDateSolar: null,
        deathDateLunar: null,
        deathPlace: null,
        hometown: null,
      },
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Yêu cầu thay đổi 456')).toBeInTheDocument();
      expect(screen.getByText('Draft')).toBeInTheDocument();
      expect(screen.getByText('123')).toBeInTheDocument();
      expect(screen.getByText('Fix name')).toBeInTheDocument();
      expect(screen.getByText('Correct Name')).toBeInTheDocument();
      expect(screen.getByText('Xem quy trình')).toHaveAttribute('href', '/workflow/instances/999');
      expect(screen.getByText('Xem khách hàng đích')).toHaveAttribute('href', '/customers/123');
    });
  });
});
