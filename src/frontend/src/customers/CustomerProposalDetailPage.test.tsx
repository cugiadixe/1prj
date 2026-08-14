import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomerProposalDetailPage from './CustomerProposalDetailPage';
import { getCustomerProposalById } from './customerProposalApi';

vi.mock('./customerProposalApi', () => ({
  getCustomerProposalById: vi.fn(),
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

describe('CustomerProposalDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  const renderComponent = (id: string = '1') =>
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/customers/proposals/${id}`]}>
          <Routes>
            <Route path="/customers/proposals/:id" element={<CustomerProposalDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

  it('renders loading state initially', () => {
    vi.mocked(getCustomerProposalById).mockReturnValue(new Promise(() => {}));
    renderComponent('1');
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('renders safe metadata and workflow links without sensitive data', async () => {
    vi.mocked(getCustomerProposalById).mockResolvedValueOnce({
      id: 1,
      processCode: 'CREATE_CUSTOMER',
      requesterId: 1,
      companyId: null,
      requestStatus: 'PENDING_APPROVAL',
      workflowInstanceId: 42,
      createdCustomerId: null,
      createdAt: new Date().toISOString(),
      updatedAt: null,
      rowVersion: 'v1',
      summary: {
        customerCode: 'CUST-001',
        fullName: 'Safe Customer Name',
        companyId: null,
      },
    });

    renderComponent('1');

    await waitFor(() => {
      expect(screen.getByTestId('customer-proposal-detail-page')).toBeInTheDocument();
    });

    expect(screen.getByText('CUST-001')).toBeInTheDocument();
    expect(screen.getByText('Safe Customer Name')).toBeInTheDocument();
    expect(screen.getByText('Xem quy trình')).toBeInTheDocument();
    
    // Ensure sensitive fields do not render (these aren't even typed in the mock)
    expect(screen.queryByText(/cccd/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/phone/i)).not.toBeInTheDocument();
  });
});
