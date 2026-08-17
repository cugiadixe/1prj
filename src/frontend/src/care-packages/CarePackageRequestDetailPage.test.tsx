import { render, screen } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CarePackageRequestDetailPage from './CarePackageRequestDetailPage';
import * as hooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./hooks');
vi.mock('../auth/AuthProvider');

const renderPage = (id: string = '1') => {
  return render(
    <BrowserRouter>
      <Routes>
        <Route path="/care-packages/:id" element={<CarePackageRequestDetailPage />} />
      </Routes>
    </BrowserRouter>,
    { wrapper: ({ children }) => {
      window.history.pushState({}, '', `/care-packages/${id}`);
      return <>{children}</>;
    }},
  );
};

const mockRequestData = {
  id: 1,
  companyId: 100,
  customerId: 10,
  status: 'Draft',
  requiresApproval: true,
  workflowInstanceId: null,
  serviceId: 5,
  saleDate: '2026-01-15',
  subtotalAmount: 500000,
  discountAmount: 0,
  discountReason: null,
  totalAmount: 500000,
  paymentTransactionId: null,
  previousRequestId: null,
  createdAt: '2026-01-15T10:00:00Z',
  createdByUserId: 1,
  updatedAt: null,
  updatedByUserId: null,
  items: [
    {
      id: 1,
      carePackageRequestId: 1,
      graveId: 'G-001',
      cotCountSnapshot: 2,
      servicePeriodStartDate: '2026-01-01',
      servicePeriodEndDate: '2026-12-31',
      unitPriceSnapshot: 250000,
      lineSubtotal: 500000,
      notes: null,
    },
  ],
};

describe('CarePackageRequestDetailPage', () => {
  let mockHasPermission: any;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHasPermission = vi.fn().mockReturnValue(true);
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: mockHasPermission,
    });
    vi.spyOn(hooks, 'useSubmitCarePackageRequest').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useApproveCarePackageRequest').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useRejectCarePackageRequest').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useCreateCarePackagePayment').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useActivateCarePackageRequest').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useCarePackagePaymentStatus').mockReturnValue({ data: undefined } as any);
  });

  it('renders loading state', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('detail-loading')).toBeInTheDocument();
  });

  it('renders detail page with data', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: mockRequestData,
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('care-package-detail-page')).toBeInTheDocument();
    expect(screen.getByTestId('status-badge')).toHaveTextContent('Nháp');
    expect(screen.getByTestId('total-amount')).toBeInTheDocument();
    expect(screen.getByTestId('line-items-table')).toBeInTheDocument();
  });

  it('shows submit button for Draft status with requiresApproval', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: mockRequestData,
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-submit')).toBeInTheDocument();
  });

  it('shows approve/reject buttons for PendingApproval status', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: { ...mockRequestData, status: 'PendingApproval', workflowInstanceId: 42 },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-approve')).toBeInTheDocument();
    expect(screen.getByTestId('btn-reject')).toBeInTheDocument();
  });

  it('shows create payment button for PaymentEligible status', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: { ...mockRequestData, status: 'PaymentEligible' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-create-payment')).toBeInTheDocument();
  });

  it('shows activate button for Paid status', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: { ...mockRequestData, status: 'Paid', paymentTransactionId: 99 },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-activate')).toBeInTheDocument();
  });

  it('hides action buttons when permissions missing', () => {
    mockHasPermission.mockReturnValue(false);
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: mockRequestData,
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.queryByTestId('btn-submit')).not.toBeInTheDocument();
  });

  it('renders permission denied on 403', () => {
    const error: any = new Error();
    error.isAxiosError = true;
    error.response = { status: 403 };

    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: undefined,
      isLoading: false,
      error,
    } as any);

    renderPage();
    expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
  });

  it('renders error state on non-403 error', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Server Error'),
    } as any);

    renderPage();
    expect(screen.getByTestId('detail-error')).toBeInTheDocument();
  });

  it('displays payment status when paymentTransactionId exists', () => {
    vi.spyOn(hooks, 'useCarePackageRequest').mockReturnValue({
      data: { ...mockRequestData, status: 'PendingPayment', paymentTransactionId: 99 },
      isLoading: false,
      error: null,
    } as any);
    vi.spyOn(hooks, 'useCarePackagePaymentStatus').mockReturnValue({
      data: { status: 'Pending' },
    } as any);

    renderPage();
    expect(screen.getByTestId('payment-status-badge')).toHaveTextContent('Pending');
  });
});
