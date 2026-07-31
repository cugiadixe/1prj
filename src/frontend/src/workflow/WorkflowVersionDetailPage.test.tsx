import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowVersionDetailPage from './WorkflowVersionDetailPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./workflowApi', () => ({
  getVersionById: vi.fn(),
  createStep: vi.fn(),
  updateStep: vi.fn(),
  deleteStep: vi.fn(),
  createApproverRule: vi.fn(),
  publishVersion: vi.fn(),
  activateVersion: vi.fn(),
  retireVersion: vi.fn(),
  deleteVersion: vi.fn(),
}));

let mockHasPermission = vi.fn();

import { getVersionById } from './workflowApi';
const mockGetVersion = vi.mocked(getVersionById);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (defId = '1', verId = '1') =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/workflow/definitions/${defId}/versions/${verId}`]}>
        <Routes>
          <Route path="/workflow/definitions/:definitionId/versions/:versionId" element={<WorkflowVersionDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

const mockDraftVersion = {
  id: 1,
  workflowDefinitionId: 1,
  versionNumber: 1,
  versionStatus: 'DRAFT',
  effectiveFrom: null,
  effectiveTo: null,
  publishedAt: null,
  rowVersion: 'AA',
  createdAt: '2026-01-01',
  steps: [],
  conditions: [],
};

const mockActiveVersion = {
  ...mockDraftVersion,
  versionStatus: 'ACTIVE',
  effectiveFrom: '2026-01-01',
};

const mockPublishedVersion = {
  ...mockDraftVersion,
  versionStatus: 'PUBLISHED',
  effectiveFrom: '2026-01-01',
};

describe('WorkflowVersionDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('renders version detail', async () => {
    mockGetVersion.mockResolvedValue(mockDraftVersion);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('workflow-version-detail-page')).toBeInTheDocument();
    });
  });

  it('shows DRAFT status tag', async () => {
    mockGetVersion.mockResolvedValue(mockDraftVersion);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('DRAFT').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows publish button for DRAFT when user has WORKFLOW_PUBLISH', async () => {
    mockGetVersion.mockResolvedValue(mockDraftVersion);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_PUBLISH');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('publish-btn')).toBeInTheDocument();
    });
  });

  it('hides publish button without WORKFLOW_PUBLISH', async () => {
    mockGetVersion.mockResolvedValue(mockDraftVersion);
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('publish-btn')).not.toBeInTheDocument();
    });
  });

  it('shows activate button for PUBLISHED version with WORKFLOW_PUBLISH', async () => {
    mockGetVersion.mockResolvedValue(mockPublishedVersion);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_PUBLISH');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('activate-btn')).toBeInTheDocument();
    });
  });

  it('shows retire button for ACTIVE version with WORKFLOW_PUBLISH', async () => {
    mockGetVersion.mockResolvedValue(mockActiveVersion);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_PUBLISH');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('retire-btn')).toBeInTheDocument();
    });
  });

  it('shows version freeze notice for ACTIVE version', async () => {
    mockGetVersion.mockResolvedValue(mockActiveVersion);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('version-freeze-notice')).toBeInTheDocument();
    });
  });

  it('shows add step button for DRAFT with WORKFLOW_CONFIG_MANAGE', async () => {
    mockGetVersion.mockResolvedValue(mockDraftVersion);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_CONFIG_MANAGE');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('add-step-btn')).toBeInTheDocument();
    });
  });

  it('hides add step button for non-DRAFT version', async () => {
    mockGetVersion.mockResolvedValue(mockActiveVersion);
    mockHasPermission.mockImplementation((p: string) => p === 'WORKFLOW_CONFIG_MANAGE');
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('add-step-btn')).not.toBeInTheDocument();
    });
  });

  it('shows steps table when steps exist', async () => {
    mockGetVersion.mockResolvedValue({
      ...mockDraftVersion,
      steps: [
        { id: 1, stepOrder: 1, stepName: 'Review', description: null, isRequired: true, dueDurationMinutes: null, rowVersion: 'BB', approverRules: [] },
      ],
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('steps-table')).toBeInTheDocument();
      expect(screen.getByText('Review')).toBeInTheDocument();
    });
  });

  it('shows empty steps message', async () => {
    mockGetVersion.mockResolvedValue(mockDraftVersion);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('steps-empty')).toBeInTheDocument();
    });
  });

  it('shows conditions read-only when conditions exist', async () => {
    mockGetVersion.mockResolvedValue({
      ...mockDraftVersion,
      conditions: [
        { id: 1, fieldCode: 'amount', operator: 'GT', value: '1000' },
      ],
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('conditions-display')).toBeInTheDocument();
      expect(screen.getByText('amount')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetVersion.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows error on fetch failure', async () => {
    mockGetVersion.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('version-detail-error')).toBeInTheDocument();
    });
  });
});
