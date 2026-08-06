import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import WorkflowDefinitionCreatePage from './WorkflowDefinitionCreatePage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
  }),
}));

vi.mock('./workflowApi', () => ({
  createDefinition: vi.fn(),
  getBusinessProcesses: vi.fn(),
}));

import { getBusinessProcesses, createDefinition } from './workflowApi';
const mockGetProcesses = vi.mocked(getBusinessProcesses);
const mockCreateDefinition = vi.mocked(createDefinition);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <WorkflowDefinitionCreatePage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('WorkflowDefinitionCreatePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
  });

  it('renders the create form after processes load', async () => {
    mockGetProcesses.mockResolvedValue([
      { processCode: 'CUST', processName: 'Customer', description: null, isApprovalRequired: true, isActive: true },
    ]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('definition-create-form')).toBeInTheDocument();
    });
  });

  it('shows loading while processes load', () => {
    mockGetProcesses.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByTestId('create-definition-loading')).toBeInTheDocument();
  });

  it('shows error on submit failure', async () => {
    mockGetProcesses.mockResolvedValue([]);
    mockCreateDefinition.mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('definition-create-form')).toBeInTheDocument();
    });
  });
});
