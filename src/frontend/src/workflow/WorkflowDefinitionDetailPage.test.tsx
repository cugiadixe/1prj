import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowDefinitionDetailPage from './WorkflowDefinitionDetailPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./workflowApi', () => ({
  getDefinitionById: vi.fn(),
  getVersionsByDefinition: vi.fn(),
}));

let mockHasPermission = vi.fn();

import { getDefinitionById, getVersionsByDefinition } from './workflowApi';
const mockGetDefinition = vi.mocked(getDefinitionById);
const mockGetVersions = vi.mocked(getVersionsByDefinition);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (id = '1') =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/workflow/definitions/${id}`]}>
        <Routes>
          <Route path="/workflow/definitions/:definitionId" element={<WorkflowDefinitionDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

const mockDef = {
  id: 1,
  definitionCode: 'WF01',
  definitionName: 'Test',
  description: 'Desc',
  processCode: 'CUST',
  isActive: true,
  rowVersion: 'AA',
  createdAt: '2026-01-01',
  updatedAt: null,
};

describe('WorkflowDefinitionDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('renders definition detail', async () => {
    mockGetDefinition.mockResolvedValue(mockDef);
    mockGetVersions.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('workflow-definition-detail-page')).toBeInTheDocument();
      expect(screen.getByText('WF01')).toBeInTheDocument();
    });
  });

  it('shows edit button with WORKFLOW_CONFIG_MANAGE', async () => {
    mockGetDefinition.mockResolvedValue(mockDef);
    mockGetVersions.mockResolvedValue([]);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_CONFIG_MANAGE');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('edit-definition-btn')).toBeInTheDocument();
    });
  });

  it('hides edit button without WORKFLOW_CONFIG_MANAGE', async () => {
    mockGetDefinition.mockResolvedValue(mockDef);
    mockGetVersions.mockResolvedValue([]);
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('edit-definition-btn')).not.toBeInTheDocument();
    });
  });

  it('shows versions table when versions exist', async () => {
    mockGetDefinition.mockResolvedValue(mockDef);
    mockGetVersions.mockResolvedValue([
      { id: 1, versionNumber: 1, versionStatus: 'DRAFT', effectiveFrom: null, effectiveTo: null, createdAt: '2026-01-01' },
    ]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('versions-table')).toBeInTheDocument();
    });
  });

  it('shows empty versions message', async () => {
    mockGetDefinition.mockResolvedValue(mockDef);
    mockGetVersions.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('versions-empty')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetDefinition.mockRejectedValue({ response: { status: 403 } });
    mockGetVersions.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows error on fetch failure', async () => {
    mockGetDefinition.mockRejectedValue(new Error('fail'));
    mockGetVersions.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('definition-detail-error')).toBeInTheDocument();
    });
  });
});
