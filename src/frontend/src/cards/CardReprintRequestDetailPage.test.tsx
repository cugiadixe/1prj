import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CardReprintRequestDetailPage from './CardReprintRequestDetailPage';
import * as hooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./hooks');
vi.mock('../auth/AuthProvider');

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ id: '1' }),
    useNavigate: () => mockNavigate,
  };
});

const renderPage = () => {
  return render(
    <BrowserRouter>
      <CardReprintRequestDetailPage />
    </BrowserRouter>
  );
};

describe('CardReprintRequestDetailPage', () => {
  let mockHasPermission: any;
  let mutateSubmitAsync: any;
  let mutatePrintInitialAsync: any;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHasPermission = vi.fn().mockReturnValue(true);
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: mockHasPermission,
    });

    mutateSubmitAsync = vi.fn();
    mutatePrintInitialAsync = vi.fn();

    vi.spyOn(hooks, 'useSubmitCardReprintRequest').mockReturnValue({ mutateAsync: mutateSubmitAsync, isPending: false } as any);
    vi.spyOn(hooks, 'usePrintInitialCardReprint').mockReturnValue({ mutateAsync: mutatePrintInitialAsync, isPending: false } as any);
    vi.spyOn(hooks, 'useCreatePaymentForCardReprint').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useMarkCardPrinted').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useMarkCardReleased').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(hooks, 'useCardReprintPaymentStatus').mockReturnValue({ data: undefined } as any);
  });

  it('renders loading state', () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('detail-loading')).toBeInTheDocument();
  });

  it('renders REPRINT DRAFT with Gửi button (no in-page approve)', () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'DRAFT', requestType: 'REPRINT', reprintNumber: 2, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('status-badge')).toHaveTextContent('DRAFT');
    expect(screen.getByTestId('btn-submit')).toBeInTheDocument();
    expect(screen.queryByTestId('btn-print-initial')).not.toBeInTheDocument();
    expect(screen.queryByTestId('btn-open-approval')).not.toBeInTheDocument();
  });

  it('renders INITIAL DRAFT with print-initial button (no submit)', () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'DRAFT', requestType: 'INITIAL_PRINT', reprintNumber: 1, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-print-initial')).toBeInTheDocument();
    expect(screen.queryByTestId('btn-submit')).not.toBeInTheDocument();
  });

  it('submits a reprint request', async () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'DRAFT', requestType: 'REPRINT', reprintNumber: 2, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    fireEvent.click(screen.getByTestId('btn-submit'));

    await waitFor(() => {
      expect(mutateSubmitAsync).toHaveBeenCalledWith({ id: 1, data: { rowVersion: 'v1' } });
    });
  });

  it('prints initial directly', async () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'DRAFT', requestType: 'INITIAL_PRINT', reprintNumber: 1, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    fireEvent.click(screen.getByTestId('btn-print-initial'));

    await waitFor(() => {
      expect(mutatePrintInitialAsync).toHaveBeenCalledWith(1);
    });
  });

  it('links to the workflow instance when PENDING_APPROVAL', () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'PENDING_APPROVAL', requestType: 'REPRINT', reprintNumber: 2, workflowInstanceId: 10, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('status-badge')).toHaveTextContent('PENDING_APPROVAL');
    expect(screen.queryByTestId('btn-submit')).not.toBeInTheDocument();
    const openBtn = screen.getByTestId('btn-open-approval');
    expect(openBtn).toBeInTheDocument();
    fireEvent.click(openBtn);
    expect(mockNavigate).toHaveBeenCalledWith('/workflow/instances/10');
  });

  it('renders permission denied if API returns 403', () => {
    const error: any = new Error();
    error.isAxiosError = true;
    error.response = { status: 403 };

    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: undefined,
      isLoading: false,
      error,
    } as any);

    renderPage();
    expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
  });
});
