import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowActionHistoryPanel from './WorkflowActionHistoryPanel';
import { formatUtcDateTime } from '../utils/datetime';

vi.mock('./workflowRuntimeApi', () => ({
  getInstanceActions: vi.fn(),
}));

import { getInstanceActions } from './workflowRuntimeApi';
const mockGetInstanceActions = vi.mocked(getInstanceActions);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPanel = (instanceId = 1) =>
  render(
    <QueryClientProvider client={queryClient}>
      <WorkflowActionHistoryPanel instanceId={instanceId} />
    </QueryClientProvider>,
  );

const mockAction = {
  id: 1,
  workflowInstanceStepId: 10,
  workflowInstanceId: 1,
  // Backend ghi ActionType ở thì hiện tại ('APPROVE'), không phải 'APPROVED'.
  actionType: 'APPROVE',
  actedBy: 5,
  actedByName: null,
  onBehalfOf: null,
  onBehalfOfName: null,
  reason: 'Looks good',
  comment: null,
  createdAt: '2026-01-02T10:00:00Z',
};

describe('WorkflowActionHistoryPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders action history with safe data', async () => {
    mockGetInstanceActions.mockResolvedValue([mockAction]);
    renderPanel();
    await waitFor(() => {
      expect(screen.getByTestId('action-history')).toBeInTheDocument();
      expect(screen.getByTestId('action-history-table')).toBeInTheDocument();
      // Loại hành động và người thực hiện nay hiển thị nhãn tiếng Việt.
      expect(screen.getByText('Duyệt')).toBeInTheDocument();
      expect(screen.getByText('Người dùng 5')).toBeInTheDocument();
      expect(screen.getByText('Looks good')).toBeInTheDocument();
      // Thời gian format qua chính hàm của ứng dụng để không phụ thuộc múi giờ máy chạy test.
      expect(screen.getByText(formatUtcDateTime(mockAction.createdAt))).toBeInTheDocument();
    });
  });

  it('shows loading state', () => {
    mockGetInstanceActions.mockReturnValue(new Promise(() => {}));
    renderPanel();
    expect(screen.getByTestId('action-history-loading')).toBeInTheDocument();
  });

  it('shows empty state when no actions', async () => {
    mockGetInstanceActions.mockResolvedValue([]);
    renderPanel();
    await waitFor(() => {
      expect(screen.getByTestId('action-history-empty')).toBeInTheDocument();
    });
  });

  it('shows error state on fetch failure', async () => {
    mockGetInstanceActions.mockRejectedValue(new Error('fail'));
    renderPanel();
    await waitFor(() => {
      expect(screen.getByTestId('action-history-error')).toBeInTheDocument();
    });
  });

  it('does not render raw PayloadJson or BeforeDataJson', async () => {
    mockGetInstanceActions.mockResolvedValue([mockAction]);
    renderPanel();
    await waitFor(() => {
      expect(screen.getByTestId('action-history-table')).toBeInTheDocument();
    });
    expect(screen.queryByText('PayloadJson')).not.toBeInTheDocument();
    expect(screen.queryByText('BeforeDataJson')).not.toBeInTheDocument();
  });
});
