
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import CustomerMasterChangeRequestsPage from './CustomerMasterChangeRequestsPage';
import { getMyCustomerMasterChangeRequests } from './customerMasterChangeApi';

vi.mock('./customerMasterChangeApi', () => ({
  getMyCustomerMasterChangeRequests: vi.fn(),
}));

describe('CustomerMasterChangeRequestsPage', () => {
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
        <MemoryRouter>
          <CustomerMasterChangeRequestsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('renders loading state initially', () => {
    vi.mocked(getMyCustomerMasterChangeRequests).mockImplementation(
      () => new Promise(() => {})
    );
    renderComponent();
    expect(screen.getByText('Danh sách khách hàng yêu cầu thay đổi')).toBeInTheDocument();
  });

  it('renders error state', async () => {
    vi.mocked(getMyCustomerMasterChangeRequests).mockRejectedValueOnce(
      new Error('Failed to fetch')
    );
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Đã xảy ra lỗi. Vui lòng thử lại.')).toBeInTheDocument();
    });
  });

  it('renders list of requests', async () => {
    vi.mocked(getMyCustomerMasterChangeRequests).mockResolvedValueOnce([
      {
        id: 101,
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
        payload: null,
      },
    ]);

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('CUSTOMER_MASTER_CHANGE')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('KH0000123')).toBeInTheDocument();
      expect(screen.getByText('Draft')).toBeInTheDocument();
      expect(screen.getByText('Xem trạng thái')).toHaveAttribute('href', '/customers/change-requests/101');
      expect(screen.getByText('Xem quy trình')).toHaveAttribute('href', '/workflow/instances/999');
    });
  });

  it('filters requests by search keyword', async () => {
    vi.mocked(getMyCustomerMasterChangeRequests).mockResolvedValueOnce([
      {
        id: 101,
        processCode: 'CUSTOMER_MASTER_CHANGE',
        requesterId: 1,
        companyId: null,
        requestStatus: 'SUBMITTED',
        workflowInstanceId: 999,
        targetCustomerId: 123,
        targetCustomerCode: 'KH0000123',
        targetCustomerName: 'Nguyễn Văn A',
        targetRowVersion: 'v1',
        createdAt: '2023-01-01T00:00:00Z',
        updatedAt: null,
        rowVersion: 'v2',
        payload: null,
      },
      {
        id: 102,
        processCode: 'CUSTOMER_MASTER_CHANGE',
        requesterId: 1,
        companyId: null,
        requestStatus: 'EXECUTED',
        workflowInstanceId: 1000,
        targetCustomerId: 456,
        targetCustomerCode: 'KH0000456',
        targetCustomerName: 'Trần Thị B',
        targetRowVersion: 'v1',
        createdAt: '2023-01-02T00:00:00Z',
        updatedAt: null,
        rowVersion: 'v2',
        payload: null,
      },
    ]);

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('Trần Thị B')).toBeInTheDocument();
    });

    const search = screen.getByPlaceholderText('Tìm theo mã hồ sơ, mã/tên KH, lý do...');
    fireEvent.change(search, { target: { value: 'KH0000456' } });

    await waitFor(() => {
      expect(screen.queryByText('Nguyễn Văn A')).not.toBeInTheDocument();
      expect(screen.getByText('Trần Thị B')).toBeInTheDocument();
    });
  });
});
