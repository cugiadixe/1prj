import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerMergeRequestDetailPage from './CustomerMergeRequestDetailPage';
import { getMergeRequestById } from './customerMergeApi';

vi.mock('./customerMergeApi', () => ({
  getMergeRequestById: vi.fn(),
}));

describe('CustomerMergeRequestDetailPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.resetAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const renderPage = (id = 'test-guid') => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/customers/merge-requests/${id}`]}>
          <Routes>
            <Route
              path="/customers/merge-requests/:id"
              element={<CustomerMergeRequestDetailPage />}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );
  };

  it('renders loading state', () => {
    vi.mocked(getMergeRequestById).mockImplementation(
      () => new Promise(() => {}),
    );
    renderPage();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('renders error state', async () => {
    vi.mocked(getMergeRequestById).mockRejectedValueOnce({
      response: { status: 404 },
    });
    renderPage();

    await waitFor(() => {
      expect(screen.getByTestId('error-alert')).toBeInTheDocument();
    });
  });

  it('renders merge request detail', async () => {
    vi.mocked(getMergeRequestById).mockResolvedValueOnce({
      id: 'test-guid',
      sourceCustomerId: 1,
      targetCustomerId: 2,
      requesterId: 10,
      requestStatus: 'EXECUTED',
      survivorshipPayload: '{}',
      sourceRowVersionSnapshot: 'a',
      targetRowVersionSnapshot: 'b',
      workflowInstanceId: 100,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-02T00:00:00Z',
      rowVersion: 'rv1',
      candidates: [
        {
          candidateCustomerId: 1,
          matchType: 'MANUAL',
          matchConfidence: null,
          snapshotPayload: null,
        },
      ],
    });

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByTestId('customer-merge-request-detail-page'),
      ).toBeInTheDocument();
      expect(screen.getByText('EXECUTED')).toBeInTheDocument();
      expect(screen.getByText('test-guid')).toBeInTheDocument();
      expect(screen.getByTestId('candidates-table')).toBeInTheDocument();
      expect(screen.getByText('MANUAL')).toBeInTheDocument();
    });
  });

  it('shows workflow link when workflowInstanceId exists', async () => {
    vi.mocked(getMergeRequestById).mockResolvedValueOnce({
      id: 'test-guid',
      sourceCustomerId: 1,
      targetCustomerId: 2,
      requesterId: 10,
      requestStatus: 'SUBMITTED',
      survivorshipPayload: '{}',
      sourceRowVersionSnapshot: 'a',
      targetRowVersionSnapshot: 'b',
      workflowInstanceId: 200,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'rv1',
      candidates: [],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Xem quy trình')).toHaveAttribute(
        'href',
        '/workflow/instances/200',
      );
    });
  });

  it('does not expose raw survivorshipPayload', async () => {
    vi.mocked(getMergeRequestById).mockResolvedValueOnce({
      id: 'test-guid',
      sourceCustomerId: 1,
      targetCustomerId: 2,
      requesterId: 10,
      requestStatus: 'DRAFT',
      survivorshipPayload: '{"secret":"value"}',
      sourceRowVersionSnapshot: 'a',
      targetRowVersionSnapshot: 'b',
      workflowInstanceId: null,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'rv1',
      candidates: [],
    });

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByTestId('customer-merge-request-detail-page'),
      ).toBeInTheDocument();
    });

    expect(screen.queryByText('{"secret":"value"}')).not.toBeInTheDocument();
  });
});
