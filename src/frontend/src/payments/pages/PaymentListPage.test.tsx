import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import PaymentListPage from './PaymentListPage';
import * as paymentApi from '../paymentApi';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

vi.mock('../../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
  }),
}));

vi.mock('../paymentApi', () => ({
  listPayments: vi.fn(),
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const renderComponent = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <PaymentListPage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('PaymentListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders correctly with data', async () => {
    vi.mocked(paymentApi.listPayments).mockResolvedValue({
      items: [
        {
          id: 1,
          billCode: 'BILL-123',
          companyId: 1,
          customerId: 10,
          paymentMethod: 'CASH',
          paymentDate: '2026-08-01',
          totalAmount: 100000,
          status: 'CONFIRMED',
          createdAt: '2026-08-01T10:00:00Z',
        }
      ],
      totalCount: 1,
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('payment-list-table')).toBeInTheDocument();
    });

    expect(screen.getByText('BILL-123')).toBeInTheDocument();
    expect(screen.getByText('100.000 VND')).toBeInTheDocument();
  });

  it('handles permission denied error (403) safely', async () => {
    vi.mocked(paymentApi.listPayments).mockRejectedValue({
      response: { status: 403 }
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
    // Ensure no raw SQL error is displayed
    expect(screen.queryByText(/SQL/i)).not.toBeInTheDocument();
  });

  it('handles empty state', async () => {
    vi.mocked(paymentApi.listPayments).mockResolvedValue({
      items: [],
      totalCount: 0,
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('payment-list-empty')).toBeInTheDocument();
    });
  });
});
