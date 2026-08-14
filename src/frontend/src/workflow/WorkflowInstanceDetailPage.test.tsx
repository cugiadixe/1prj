import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowInstanceDetailPage from './WorkflowInstanceDetailPage';

let mockHasPermission = vi.fn();
const mockUseAuth = vi.fn();

vi.mock('../auth/AuthProvider', () => ({
  useAuth: () => mockUseAuth(),
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./workflowRuntimeApi', () => ({
  getInstance: vi.fn(),
  approveStep: vi.fn(),
  returnStep: vi.fn(),
  resubmitInstance: vi.fn(),
  withdrawInstance: vi.fn(),
  reassignStep: vi.fn(),
  rejectStep: vi.fn(),
  retryExecution: vi.fn(),
  getInstanceActions: vi.fn(),
}));

vi.mock('./WorkflowActionHistoryPanel', () => ({
  default: () => <div data-testid="action-history">Action History</div>,
}));

import { getInstance } from './workflowRuntimeApi';
const mockGetInstance = vi.mocked(getInstance);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (instanceId = '1') =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/workflow/instances/${instanceId}`]}>
        <Routes>
          <Route path="/workflow/instances/:instanceId" element={<WorkflowInstanceDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

const mockInstance = {
  id: 1,
  workflowVersionId: 10,
  processCode: 'CUST',
  companyId: null,
  requesterId: 99,
  requesterName: 'Người đề xuất',
  businessEntityType: 'Customer',
  businessEntityId: 100,
  businessEntityLabel: null,
  instanceStatus: 'PENDING_APPROVAL',
  roundNo: 1,
  rowVersion: 'AA',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
  steps: [
    {
      id: 1,
      stepOrder: 1,
      stepName: 'Manager Review',
      roundNo: 1,
      stepStatus: 'PENDING',
      assignedAt: '2026-01-01T00:00:00Z',
      completedAt: null,
      completedBy: null,
      completedByName: null,
      rowVersion: 'BB',
      assignees: [{ userId: 5, userName: 'Người duyệt', approverSourceType: 'ROLE' }],
    },
  ],
};

describe('WorkflowInstanceDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
    mockUseAuth.mockReturnValue({ user: { userId: 5, username: 'approver', displayName: 'Approver' } });
  });

  it('renders instance detail', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
  });

  it('shows instance status tag', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-status-tag')).toBeInTheDocument();
      // Trạng thái nay hiển thị nhãn tiếng Việt thay vì mã thô.
      expect(screen.getByTestId('instance-status-tag')).toHaveTextContent('Chờ duyệt');
    });
  });

  it('shows version snapshot notice', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('version-snapshot-notice')).toBeInTheDocument();
    });
  });

  it('shows instance metadata', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-metadata')).toBeInTheDocument();
      expect(screen.getByText('CUST')).toBeInTheDocument();
      expect(screen.getByText('Customer')).toBeInTheDocument();
    });
  });

  it('shows steps table', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('steps-table')).toBeInTheDocument();
      expect(screen.getByText('Manager Review')).toBeInTheDocument();
    });
  });

  it('shows approve button when user is assignee and not requester', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 5, username: 'approver', displayName: 'Approver' } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('approve-btn-1')).toBeInTheDocument();
    });
  });

  it('hides approve button when user is requester', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 99, username: 'requester', displayName: 'Requester' } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('approve-btn-1')).not.toBeInTheDocument();
  });

  it('hides approve button when user is not assignee', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 999, username: 'other', displayName: 'Other' } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('approve-btn-1')).not.toBeInTheDocument();
  });

  it('shows return button when user is assignee', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('return-btn-1')).toBeInTheDocument();
    });
  });

  it('shows resubmit button when user is requester and instance is RETURNED', async () => {
    mockGetInstance.mockResolvedValue({ ...mockInstance, instanceStatus: 'RETURNED' });
    mockUseAuth.mockReturnValue({ user: { userId: 99, username: 'requester', displayName: 'Requester' } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('resubmit-btn')).toBeInTheDocument();
    });
  });

  it('hides resubmit button when user is not requester', async () => {
    mockGetInstance.mockResolvedValue({ ...mockInstance, instanceStatus: 'RETURNED' });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('resubmit-btn')).not.toBeInTheDocument();
  });

  it('shows withdraw button when user is requester and instance is PENDING_APPROVAL', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 99, username: 'requester', displayName: 'Requester' } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('withdraw-btn')).toBeInTheDocument();
    });
  });

  it('hides withdraw button when instance is COMPLETED', async () => {
    mockGetInstance.mockResolvedValue({ ...mockInstance, instanceStatus: 'COMPLETED' });
    mockUseAuth.mockReturnValue({ user: { userId: 99, username: 'requester', displayName: 'Requester' } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('withdraw-btn')).not.toBeInTheDocument();
  });

  it('shows reassign button when user has WORKFLOW_REASSIGN_PENDING and step is PENDING', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_REASSIGN_PENDING');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('reassign-btn-1')).toBeInTheDocument();
    });
  });

  it('hides reassign button without WORKFLOW_REASSIGN_PENDING', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('reassign-btn-1')).not.toBeInTheDocument();
  });

  it('shows permission denied on 403', async () => {
    mockGetInstance.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows error on fetch failure', async () => {
    mockGetInstance.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-error')).toBeInTheDocument();
    });
  });

  it('renders action history panel', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('action-history')).toBeInTheDocument();
    });
  });

  it('shows reject button when user has WORKFLOW_REJECT, is assignee, and not requester', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 5, username: 'approver', displayName: 'Approver' } });
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_REJECT');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('reject-btn-1')).toBeInTheDocument();
    });
  });

  it('hides reject button when user lacks WORKFLOW_REJECT permission', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 5, username: 'approver', displayName: 'Approver' } });
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('reject-btn-1')).not.toBeInTheDocument();
  });

  it('hides reject button when user is requester', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 99, username: 'requester', displayName: 'Requester' } });
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_REJECT');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('reject-btn-1')).not.toBeInTheDocument();
  });

  it('hides reject button when user is not assignee', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockUseAuth.mockReturnValue({ user: { userId: 999, username: 'other', displayName: 'Other' } });
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_REJECT');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('reject-btn-1')).not.toBeInTheDocument();
  });

  it('shows retry button when instance is FAILED and user has WORKFLOW_RETRY_EXECUTION', async () => {
    mockGetInstance.mockResolvedValue({ ...mockInstance, instanceStatus: 'FAILED', steps: [] });
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_RETRY_EXECUTION');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('retry-execution-btn')).toBeInTheDocument();
    });
  });

  it('hides retry button when instance is not FAILED', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_RETRY_EXECUTION');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('retry-execution-btn')).not.toBeInTheDocument();
  });

  it('hides retry button when user lacks WORKFLOW_RETRY_EXECUTION permission', async () => {
    mockGetInstance.mockResolvedValue({ ...mockInstance, instanceStatus: 'FAILED', steps: [] });
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('retry-execution-btn')).not.toBeInTheDocument();
  });

  it('does not display raw payload JSON', async () => {
    mockGetInstance.mockResolvedValue(mockInstance);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('instance-detail-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('raw-payload')).not.toBeInTheDocument();
  });
});
