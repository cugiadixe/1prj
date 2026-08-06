import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowDefinitionsPage from './WorkflowDefinitionsPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./workflowApi', () => ({
  searchDefinitions: vi.fn(),
}));

let mockHasPermission = vi.fn();

import { searchDefinitions } from './workflowApi';
const mockSearchDefinitions = vi.mocked(searchDefinitions);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <WorkflowDefinitionsPage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('WorkflowDefinitionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('renders the definitions page', async () => {
    mockSearchDefinitions.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('workflow-definitions-page')).toBeInTheDocument();
    });
  });

  it('shows empty state when no definitions', async () => {
    mockSearchDefinitions.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('workflow-list-empty')).toBeInTheDocument();
    });
  });

  it('renders definition rows from API', async () => {
    mockSearchDefinitions.mockResolvedValue({
      items: [
        { id: 1, definitionCode: 'WF01', definitionName: 'Test WF', processCode: 'CUST', isActive: true, createdAt: '2026-01-01' },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('WF01')).toBeInTheDocument();
      expect(screen.getByText('Test WF')).toBeInTheDocument();
    });
  });

  it('shows create button with WORKFLOW_CONFIG_MANAGE', async () => {
    mockSearchDefinitions.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_CONFIG_MANAGE');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('create-definition-btn')).toBeInTheDocument();
    });
  });

  it('hides create button without WORKFLOW_CONFIG_MANAGE', async () => {
    mockSearchDefinitions.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('create-definition-btn')).not.toBeInTheDocument();
    });
  });

  it('shows error state on API failure', async () => {
    mockSearchDefinitions.mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('workflow-list-error')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockSearchDefinitions.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
