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
  let mutateApproveAsync: any;
  let mutateRejectAsync: any;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHasPermission = vi.fn().mockReturnValue(true);
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: mockHasPermission,
    });

    mutateSubmitAsync = vi.fn();
    mutateApproveAsync = vi.fn();
    mutateRejectAsync = vi.fn();

    vi.spyOn(hooks, 'useSubmitCardReprintRequest').mockReturnValue({ mutateAsync: mutateSubmitAsync, isPending: false } as any);
    vi.spyOn(hooks, 'useApproveCardReprintRequest').mockReturnValue({ mutateAsync: mutateApproveAsync, isPending: false } as any);
    vi.spyOn(hooks, 'useRejectCardReprintRequest').mockReturnValue({ mutateAsync: mutateRejectAsync, isPending: false } as any);
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

  it('renders detail with DRAFT status', () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'DRAFT', rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('status-badge')).toHaveTextContent('DRAFT');
    expect(screen.getByTestId('btn-submit')).toBeInTheDocument();
    expect(screen.queryByTestId('btn-approve')).not.toBeInTheDocument();
  });

  it('submits request', async () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'DRAFT', rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    fireEvent.click(screen.getByTestId('btn-submit'));

    await waitFor(() => {
      expect(mutateSubmitAsync).toHaveBeenCalledWith({ id: 1, data: { rowVersion: 'v1' } });
    });
  });

  it('renders detail with PENDING_APPROVAL status', () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'PENDING_APPROVAL', workflowInstanceId: 10, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('status-badge')).toHaveTextContent('PENDING_APPROVAL');
    expect(screen.queryByTestId('btn-submit')).not.toBeInTheDocument();
    expect(screen.getByTestId('btn-approve')).toBeInTheDocument();
    expect(screen.getByTestId('btn-reject')).toBeInTheDocument();
  });

  it('approves request with modal', async () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'PENDING_APPROVAL', workflowInstanceId: 10, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    
    // Open modal
    fireEvent.click(screen.getByTestId('btn-approve'));
    
    // Change comment
    fireEvent.change(screen.getByTestId('input-approve-comment'), { target: { value: 'Looks good' } });
    
    // Confirm (click OK in Ant Design Modal, this is a bit tricky, but we can query by role or class)
    // Antd Modal footer OK button usually has class ant-btn-primary
    const okBtn = screen.getAllByRole('button').find(b => b.textContent === 'OK');
    if (okBtn) fireEvent.click(okBtn);

    await waitFor(() => {
      expect(mutateApproveAsync).toHaveBeenCalledWith({
        id: 1,
        data: { stepId: 10, targetVersion: 0, comment: 'Looks good' },
      });
    });
  });

  it('rejects request with modal', async () => {
    vi.spyOn(hooks, 'useCardReprintRequest').mockReturnValue({
      data: { id: 1, status: 'PENDING_APPROVAL', workflowInstanceId: 10, rowVersion: 'v1' },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    
    // Open modal
    fireEvent.click(screen.getByTestId('btn-reject'));
    
    // Change reason
    fireEvent.change(screen.getByTestId('input-reject-reason'), { target: { value: 'Bad photo' } });
    
    const okBtn = screen.getAllByRole('button').find(b => b.textContent === 'OK');
    if (okBtn) fireEvent.click(okBtn);

    await waitFor(() => {
      expect(mutateRejectAsync).toHaveBeenCalledWith({
        id: 1,
        data: { stepId: 10, targetVersion: 0, reason: 'Bad photo' },
      });
    });
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
