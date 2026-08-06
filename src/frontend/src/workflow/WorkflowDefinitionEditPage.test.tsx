import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowDefinitionEditPage from './WorkflowDefinitionEditPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
  }),
}));

vi.mock('./workflowApi', () => ({
  getDefinitionById: vi.fn(),
  updateDefinition: vi.fn(),
}));

import { getDefinitionById } from './workflowApi';
const mockGetDefinition = vi.mocked(getDefinitionById);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (id = '1') =>
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/workflow/definitions/${id}/edit`]}>
        <Routes>
          <Route path="/workflow/definitions/:definitionId/edit" element={<WorkflowDefinitionEditPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );

describe('WorkflowDefinitionEditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders edit form with pre-populated data', async () => {
    mockGetDefinition.mockResolvedValue({
      id: 1,
      definitionCode: 'WF01',
      definitionName: 'Test',
      description: 'Desc',
      processCode: 'CUST',
      isActive: true,
      rowVersion: 'AA',
      createdAt: '2026-01-01',
      updatedAt: null,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('definition-edit-form')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetDefinition.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows fetch error on failure', async () => {
    mockGetDefinition.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('definition-edit-fetch-error')).toBeInTheDocument();
    });
  });
});
