import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ServiceListPage from './ServiceListPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('../auth/CompanyProvider', () => ({
  useCompany: () => ({
    currentCompanyId: mockCurrentCompanyId,
  }),
}));

vi.mock('./servicesApi', () => ({
  searchServices: vi.fn(),
}));

let mockHasPermission = vi.fn();
let mockCurrentCompanyId: number | null = 1;
import { searchServices } from './servicesApi';
const mockSearchServices = vi.mocked(searchServices);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ServiceListPage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('ServiceListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(true);
    mockCurrentCompanyId = 1;
  });

  it('renders empty state', async () => {
    mockSearchServices.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('service-list-empty')).toBeInTheDocument();
    });
  });

  it('renders warning if no company context', async () => {
    mockCurrentCompanyId = null;
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('no-company-warning')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockSearchServices.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
