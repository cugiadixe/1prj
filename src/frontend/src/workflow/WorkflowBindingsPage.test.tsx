import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowBindingsPage from './WorkflowBindingsPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./workflowApi', () => ({
  getBindings: vi.fn(),
  getBusinessProcesses: vi.fn(),
  createBinding: vi.fn(),
  updateBinding: vi.fn(),
}));

let mockHasPermission = vi.fn();

import { getBindings, getBusinessProcesses } from './workflowApi';
const mockGetBindings = vi.mocked(getBindings);
const mockGetProcesses = vi.mocked(getBusinessProcesses);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <WorkflowBindingsPage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('WorkflowBindingsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
    mockGetProcesses.mockResolvedValue([]);
  });

  it('renders bindings page', async () => {
    mockGetBindings.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('workflow-bindings-page')).toBeInTheDocument();
    });
  });

  it('shows empty state', async () => {
    mockGetBindings.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('binding-list-empty')).toBeInTheDocument();
    });
  });

  it('renders binding rows', async () => {
    mockGetBindings.mockResolvedValue([
      {
        id: 1,
        workflowVersionId: 1,
        processCode: 'CUST',
        scopeType: 'GLOBAL',
        companyId: null,
        priority: 1,
        effectiveFrom: '2026-01-01',
        effectiveTo: null,
        isActive: true,
        rowVersion: 'AA',
      },
    ]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('CUST')).toBeInTheDocument();
    });
  });

  it('shows create button with WORKFLOW_BIND_PROCESS', async () => {
    mockGetBindings.mockResolvedValue([]);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_BIND_PROCESS');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('create-binding-btn')).toBeInTheDocument();
    });
  });

  it('hides create button without WORKFLOW_BIND_PROCESS', async () => {
    mockGetBindings.mockResolvedValue([]);
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('create-binding-btn')).not.toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetBindings.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows error on failure', async () => {
    mockGetBindings.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('binding-list-error')).toBeInTheDocument();
    });
  });
});
