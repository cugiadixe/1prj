import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowActionHistoryPanel from './WorkflowActionHistoryPanel';

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
  actionType: 'APPROVED',
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
      expect(screen.getByText('APPROVED')).toBeInTheDocument();
      expect(screen.getByText('User 5')).toBeInTheDocument();
      expect(screen.getByText('Looks good')).toBeInTheDocument();
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
