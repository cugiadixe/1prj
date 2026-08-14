import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowMyApprovalsPage from './WorkflowMyApprovalsPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(false),
  }),
}));

vi.mock('./workflowRuntimeApi', () => ({
  getMyApprovals: vi.fn(),
}));

import { getMyApprovals } from './workflowRuntimeApi';
const mockGetMyApprovals = vi.mocked(getMyApprovals);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/workflow/my-approvals']}>
        <Routes>
          <Route path="/workflow/my-approvals" element={<WorkflowMyApprovalsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

describe('WorkflowMyApprovalsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders approval list', async () => {
    mockGetMyApprovals.mockResolvedValue([
      {
        instanceId: 1,
        stepId: 10,
        processCode: 'CUST',
        requesterId: 99,
        requesterName: 'Người đề xuất',
        businessEntityType: 'Customer',
        businessEntityId: 100,
        businessEntityLabel: null,
        stepName: 'Review',
        instanceStatus: 'PENDING_APPROVAL',
        assignedAt: '2026-01-01T00:00:00Z',
      },
    ]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-approvals-table')).toBeInTheDocument();
      expect(screen.getByText('Review')).toBeInTheDocument();
    });
  });

  it('renders empty state', async () => {
    mockGetMyApprovals.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-approvals-empty')).toBeInTheDocument();
    });
  });

  it('renders error state', async () => {
    mockGetMyApprovals.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-approvals-error')).toBeInTheDocument();
    });
  });

  it('renders permission denied on 403', async () => {
    mockGetMyApprovals.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('does not render my-requests UI', async () => {
    mockGetMyApprovals.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('my-approvals-page')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('my-requests')).not.toBeInTheDocument();
  });
});
