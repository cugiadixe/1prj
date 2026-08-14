import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowMyRequestsPage from './WorkflowMyRequestsPage';

vi.mock('./workflowRuntimeApi', () => ({
  getMyRequests: vi.fn(),
}));

import { getMyRequests } from './workflowRuntimeApi';
const mockGetMyRequests = vi.mocked(getMyRequests);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <WorkflowMyRequestsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );

const mockRequest = {
  id: 1,
  workflowVersionId: 10,
  processCode: 'CREATE_CUSTOMER',
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
  steps: [],
};

describe('WorkflowMyRequestsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders page with title', async () => {
    mockGetMyRequests.mockResolvedValue([mockRequest]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-requests-page')).toBeInTheDocument();
      // Tiêu đề trang đã Việt hoá.
      expect(screen.getByText('Yêu cầu của tôi')).toBeInTheDocument();
    });
  });

  it('shows loading state', () => {
    mockGetMyRequests.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByTestId('my-requests-loading')).toBeInTheDocument();
  });

  it('shows empty state when no requests', async () => {
    mockGetMyRequests.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-requests-empty')).toBeInTheDocument();
    });
  });

  it('renders requests table with safe metadata', async () => {
    mockGetMyRequests.mockResolvedValue([mockRequest]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-requests-table')).toBeInTheDocument();
      expect(screen.getByText('CREATE_CUSTOMER')).toBeInTheDocument();
      // Trạng thái nay hiển thị nhãn tiếng Việt thay vì mã thô.
      expect(screen.getByText('Chờ duyệt')).toBeInTheDocument();
    });
  });

  it('shows error state on fetch failure', async () => {
    mockGetMyRequests.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-requests-error')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetMyRequests.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('does not render raw payload fields', async () => {
    mockGetMyRequests.mockResolvedValue([mockRequest]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-requests-table')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('raw-payload')).not.toBeInTheDocument();
  });
});
