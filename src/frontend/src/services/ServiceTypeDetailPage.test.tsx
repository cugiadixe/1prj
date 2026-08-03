import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ServiceTypeDetailPage from './ServiceTypeDetailPage';

vi.mock('../auth/AuthProvider', () => ({
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

vi.mock('./serviceTypesApi', () => ({
  getServiceTypeById: vi.fn(),
  deactivateServiceType: vi.fn(),
}));

let mockHasPermission = vi.fn();
import { getServiceTypeById } from './serviceTypesApi';
const mockGetServiceTypeById = vi.mocked(getServiceTypeById);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () =>
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/services/types/:id" element={<ServiceTypeDetailPage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>,
  );

describe('ServiceTypeDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(false);
    window.history.pushState({}, 'Test page', '/services/types/1');
  });

  it('renders details', async () => {
    mockGetServiceTypeById.mockResolvedValue({
      id: 1, code: 'T1', name: 'Test', description: null, standardPrice: 100, standardPriceCurrency: 'VND', cycleDurationMonths: null, isActive: true, createdAt: '2026-01-01', updatedAt: null, rowVersion: 'v1'
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('T1')).toBeInTheDocument();
      expect(screen.getByText('Test')).toBeInTheDocument();
    });
  });

  it('shows edit button only with SERVICE_TYPE_MANAGE', async () => {
    mockGetServiceTypeById.mockResolvedValue({ id: 1, standardPrice: 100 } as any);
    mockHasPermission.mockImplementation((p: string) => p === 'SERVICE_TYPE_MANAGE');
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('edit-service-type-btn')).toBeInTheDocument();
    });
  });

  it('shows permission denied on 403', async () => {
    mockGetServiceTypeById.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
