import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import CustomerMergeRequestsPage from './CustomerMergeRequestsPage';
import { listMergeRequests } from './customerMergeApi';

vi.mock('./customerMergeApi', () => ({
  listMergeRequests: vi.fn(),
}));

describe('CustomerMergeRequestsPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.resetAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const renderPage = () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CustomerMergeRequestsPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );
  };

  it('renders page title', () => {
    vi.mocked(listMergeRequests).mockImplementation(
      () => new Promise(() => {}),
    );
    renderPage();
    expect(screen.getByText('Merge Requests')).toBeInTheDocument();
  });

  it('renders error state', async () => {
    vi.mocked(listMergeRequests).mockRejectedValueOnce(
      new Error('Failed'),
    );
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId('list-error')).toBeInTheDocument();
    });
  });

  it('renders list of merge requests', async () => {
    vi.mocked(listMergeRequests).mockResolvedValueOnce({
      items: [
        {
          id: 'abcdef12-3456-7890-abcd-ef1234567890',
          sourceCustomerId: 1,
          targetCustomerId: 2,
          requesterId: 10,
          requestStatus: 'SUBMITTED',
          survivorshipPayload: '{}',
          sourceRowVersionSnapshot: 'a',
          targetRowVersionSnapshot: 'b',
          workflowInstanceId: 100,
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: null,
          rowVersion: 'rv1',
          candidates: [],
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('SUBMITTED')).toBeInTheDocument();
      expect(screen.getByText('View')).toHaveAttribute(
        'href',
        '/customers/merge-requests/abcdef12-3456-7890-abcd-ef1234567890',
      );
      expect(screen.getByText('Workflow')).toHaveAttribute(
        'href',
        '/workflow/instances/100',
      );
    });
  });

  it('renders empty state', async () => {
    vi.mocked(listMergeRequests).mockResolvedValueOnce({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderPage();

    await waitFor(() => {
      const table = screen.getByRole('table');
      expect(table).toBeInTheDocument();
      expect(screen.queryByText('SUBMITTED')).not.toBeInTheDocument();
    });
  });
});
