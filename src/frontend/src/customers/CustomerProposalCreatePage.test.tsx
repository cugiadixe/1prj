import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CustomerProposalCreatePage from './CustomerProposalCreatePage';
import { createCustomerProposal } from './customerProposalApi';
import { checkDuplicates } from './customersApi';

vi.mock('./customerProposalApi', () => ({
  createCustomerProposal: vi.fn(),
}));

vi.mock('./customersApi', () => ({
  checkDuplicates: vi.fn(),
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

describe('CustomerProposalCreatePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <CustomerProposalCreatePage />
        </BrowserRouter>
      </QueryClientProvider>,
    );

  it('renders form', () => {
    renderComponent();
    expect(screen.getByTestId('customer-proposal-create-page')).toBeInTheDocument();
    expect(screen.getByTestId('input-customerCode')).toBeInTheDocument();
  });

  it('submits valid proposal successfully', async () => {
    vi.mocked(createCustomerProposal).mockResolvedValueOnce({
      id: 1,
      processCode: 'CREATE_CUSTOMER',
      requesterId: 1,
      companyId: null,
      requestStatus: 'PENDING_APPROVAL',
      workflowInstanceId: 1,
      createdCustomerId: null,
      createdAt: new Date().toISOString(),
      updatedAt: null,
      rowVersion: 'v1',
      summary: null,
    });
    renderComponent();

    await userEvent.type(screen.getByTestId('input-customerCode'), 'CUST-001');
    await userEvent.type(screen.getByTestId('input-fullName'), 'Test Customer');

    await userEvent.click(screen.getByTestId('submit-create-proposal'));

    await waitFor(() => {
      expect(createCustomerProposal).toHaveBeenCalledWith(
        expect.objectContaining({
          customerCode: 'CUST-001',
          fullName: 'Test Customer',
        }),
      );
    });
  });

  it('shows error on submission failure', async () => {
    vi.mocked(createCustomerProposal).mockRejectedValueOnce(new Error('Network error'));
    renderComponent();

    await userEvent.type(screen.getByTestId('input-customerCode'), 'CUST-001');
    await userEvent.type(screen.getByTestId('input-fullName'), 'Test Customer');

    await userEvent.click(screen.getByTestId('submit-create-proposal'));

    await waitFor(() => {
      expect(screen.getByTestId('create-error')).toBeInTheDocument();
    });
  });
});
