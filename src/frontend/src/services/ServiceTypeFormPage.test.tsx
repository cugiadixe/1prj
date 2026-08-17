import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ServiceTypeFormPage from './ServiceTypeFormPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./serviceTypesApi', () => ({
  getServiceTypeById: vi.fn(),
  createServiceType: vi.fn(),
  updateServiceType: vi.fn(),
}));

let mockHasPermission = vi.fn();
import { getServiceTypeById } from './serviceTypesApi';
const mockGetServiceTypeById = vi.mocked(getServiceTypeById);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = (url = '/services/types/new') => {
  window.history.pushState({}, 'Test page', url);
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/services/types/new" element={<ServiceTypeFormPage />} />
          <Route path="/services/types/:id/edit" element={<ServiceTypeFormPage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>,
  );
};

describe('ServiceTypeFormPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(true);
  });

  it('renders create form', async () => {
    renderPage('/services/types/new');
    await waitFor(() => {
      expect(screen.getByTestId('input-code')).toBeInTheDocument();
      expect(screen.getByTestId('input-standard-price')).toBeInTheDocument();
    });
  });

  it('renders edit form with pre-populated data', async () => {
    mockGetServiceTypeById.mockResolvedValue({
      id: 1, code: 'T1', name: 'Test', description: null, standardPrice: 100, standardPriceCurrency: 'VND', cycleDurationMonths: null, pricingBasis: 'PER_COT', isActive: true, createdAt: '2026-01-01', updatedAt: null, rowVersion: 'v1'
    });
    renderPage('/services/types/1/edit');
    await waitFor(() => {
      expect(screen.queryByTestId('input-code')).not.toBeInTheDocument();
      expect(screen.getByTestId('input-name')).toBeInTheDocument();
    });
  });

  it('shows permission denied if no permission', async () => {
    mockHasPermission.mockReturnValue(false);
    renderPage('/services/types/new');
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
