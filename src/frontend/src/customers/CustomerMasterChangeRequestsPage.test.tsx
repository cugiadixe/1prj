
import { render, screen, waitFor } from '@testing-library/react';
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
    expect(screen.getByText('My Customer Change Requests')).toBeInTheDocument();
  });

  it('renders error state', async () => {
    vi.mocked(getMyCustomerMasterChangeRequests).mockRejectedValueOnce(
      new Error('Failed to fetch')
    );
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('An error occurred. Please try again.')).toBeInTheDocument();
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
        targetRowVersion: 'v1',
        createdAt: '2023-01-01T00:00:00Z',
        updatedAt: null,
        rowVersion: 'v2',
        payload: null,
      },
    ]);

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('101')).toBeInTheDocument();
      expect(screen.getByText('123')).toBeInTheDocument();
      expect(screen.getByText('Draft')).toBeInTheDocument();
      expect(screen.getByText('View Status')).toHaveAttribute('href', '/customers/change-requests/101');
      expect(screen.getByText('View Workflow')).toHaveAttribute('href', '/workflow/instances/999');
    });
  });
});
