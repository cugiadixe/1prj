import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ServiceCreatePage from './ServiceCreatePage';

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
  createService: vi.fn(),
}));

vi.mock('./serviceTypesApi', () => ({
  searchServiceTypes: vi.fn(),
}));

let mockHasPermission = vi.fn();
let mockCurrentCompanyId: number | null = 1;
import { searchServiceTypes } from './serviceTypesApi';
const mockSearchServiceTypes = vi.mocked(searchServiceTypes);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () => {
  window.history.pushState({}, 'Test page', '/services/new');
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/services/new" element={<ServiceCreatePage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>,
  );
};

describe('ServiceCreatePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(true);
    mockCurrentCompanyId = 1;
    mockSearchServiceTypes.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 100 });
  });

  it('renders create form', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('input-service-type')).toBeInTheDocument();
      expect(screen.getByTestId('input-customer-id')).toBeInTheDocument();
    });
  });

  it('shows warning if no company context', async () => {
    mockCurrentCompanyId = null;
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('no-company-warning')).toBeInTheDocument();
    });
  });

  it('shows permission denied if no permission', async () => {
    mockHasPermission.mockReturnValue(false);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
