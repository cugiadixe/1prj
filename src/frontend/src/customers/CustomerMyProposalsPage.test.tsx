import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomerMyProposalsPage from './CustomerMyProposalsPage';
import { getMyCustomerProposals } from './customerProposalApi';

vi.mock('./customerProposalApi', () => ({
  getMyCustomerProposals: vi.fn(),
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

describe('CustomerMyProposalsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <CustomerMyProposalsPage />
        </BrowserRouter>
      </QueryClientProvider>,
    );

  it('renders table with safe data', async () => {
    vi.mocked(getMyCustomerProposals).mockResolvedValueOnce([
      {
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
      }
    ]);

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('CUST-001')).toBeInTheDocument();
    });

    expect(screen.getByText('Safe Customer Name')).toBeInTheDocument();
    expect(screen.getByText('PENDING_APPROVAL')).toBeInTheDocument();
  });
});
