import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ServiceTypeListPage from './ServiceTypeListPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./serviceTypesApi', () => ({
  searchServiceTypes: vi.fn(),
}));

let mockHasPermission = vi.fn();
import { searchServiceTypes } from './serviceTypesApi';
const mockSearchServiceTypes = vi.mocked(searchServiceTypes);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ServiceTypeListPage />
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('ServiceTypeListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
  });

  it('renders the page and shows empty state', async () => {
    mockSearchServiceTypes.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('service-types-page')).toBeInTheDocument();
      expect(screen.getByTestId('service-type-list-empty')).toBeInTheDocument();
    });
  });

  it('renders rows from API', async () => {
    mockSearchServiceTypes.mockResolvedValue({
      items: [
        { id: 1, code: 'T1', name: 'Test', description: null, standardPrice: 100, standardPriceCurrency: 'VND', cycleDurationMonths: null, isActive: true, createdAt: '2026-01-01', updatedAt: null, rowVersion: 'v1' },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('T1')).toBeInTheDocument();
      expect(screen.getByText('Test')).toBeInTheDocument();
    });
  });

  it('shows create button only with SERVICE_TYPE_MANAGE', async () => {
    mockSearchServiceTypes.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    mockHasPermission.mockImplementation((p: string) => p === 'SERVICE_TYPE_MANAGE');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('create-service-type-btn')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockSearchServiceTypes.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });

  it('shows generic error on 500', async () => {
    mockSearchServiceTypes.mockRejectedValue({ response: { status: 500 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('service-type-list-error')).toBeInTheDocument();
    });
  });
});
