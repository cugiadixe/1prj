import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ReconciliationDailyPage from './ReconciliationDailyPage';
import * as reconciliationApi from '../reconciliationApi';

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

vi.mock('../reconciliationApi', () => ({
  getDailyReport: vi.fn(),
  prepareReconciliation: vi.fn(),
  confirmReconciliation: vi.fn(),
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const renderComponent = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ReconciliationDailyPage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('ReconciliationDailyPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders correctly without a period (unprepared state)', async () => {
    vi.mocked(reconciliationApi.getDailyReport).mockResolvedValue({
      companyId: 1,
      date: '2026-08-01',
      totalAmount: 50000,
      transactionCount: 2,
      payments: [
        {
          id: 1,
          billCode: 'BILL-1',
          companyId: 1,
          customerId: 1,
          paymentMethod: 'CASH',
          paymentDate: '2026-08-01',
          totalAmount: 25000,
          status: 'CONFIRMED',
          createdAt: '2026-08-01',
        },
      ]
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText(/50,000 VND/)).toBeInTheDocument();
    });

    // The status should be UNPREPARED if no period exists
    expect(screen.getByText('UNPREPARED')).toBeInTheDocument();
  });

  it('shows prepare button if period exists but not prepared', async () => {
    vi.mocked(reconciliationApi.getDailyReport).mockResolvedValue({
      companyId: 1,
      date: '2026-08-01',
      totalAmount: 50000,
      transactionCount: 2,
      period: {
        id: 10,
        companyId: 1,
        periodType: 'DAILY',
        periodDate: '2026-08-01',
        status: 'DRAFT',
        totalAmount: 50000,
        transactionCount: 2,
        rowVersion: 'v1',
      },
      payments: []
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByTestId('prepare-recon-btn')).toBeInTheDocument();
    });
  });
});
