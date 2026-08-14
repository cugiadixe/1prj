import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ServiceDetailPage from './ServiceDetailPage';

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
  getServiceById: vi.fn(),
}));

let mockHasPermission = vi.fn();
let mockCurrentCompanyId: number | null = 1;
import { getServiceById } from './servicesApi';
const mockGetServiceById = vi.mocked(getServiceById);

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () => {
  window.history.pushState({}, 'Test page', '/services/1');
  render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/services/:id" element={<ServiceDetailPage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>,
  );
};

describe('ServiceDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient.clear();
    mockHasPermission = vi.fn().mockReturnValue(true);
    mockCurrentCompanyId = 1;
  });

  it('renders details', async () => {
    mockGetServiceById.mockResolvedValue({
      id: 1, serviceTypeId: 1, serviceTypeCode: 'T1', serviceTypeName: 'Type 1', customerId: 1, customerCode: 'KH0001', customerName: 'Khách hàng 1', companyId: 1, companyName: 'Công ty 1', status: 'ACTIVE', appliedPrice: 1000, standardPriceSnapshot: 1000, isOverridePrice: false, overrideApprovalRequestId: null, validFrom: '2026-01-01', validTo: null, cycleNumber: 1, previousServiceId: null, createdAt: '2026-01-01', updatedAt: null, rowVersion: 'v1'
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Type 1')).toBeInTheDocument();
      expect(screen.getByText('ACTIVE')).toBeInTheDocument();
    });
  });

  // Xem chéo công ty là hành vi có chủ đích: backend đã kiểm quyền SERVICE_VIEW
  // theo công ty của dịch vụ, nên frontend không chặn mà vẫn hiển thị chi tiết.
  it('renders a service belonging to another company', async () => {
    mockGetServiceById.mockResolvedValue({
      id: 1, serviceTypeId: 1, serviceTypeCode: 'T1', serviceTypeName: 'Type 1', customerId: 1, customerCode: 'KH0001', customerName: 'Khách hàng 1', companyId: 2, companyName: 'Công ty 2', status: 'ACTIVE', appliedPrice: 1000, standardPriceSnapshot: 1000, isOverridePrice: false, overrideApprovalRequestId: null, validFrom: '2026-01-01', validTo: null, cycleNumber: 1, previousServiceId: null, createdAt: '2026-01-01', updatedAt: null, rowVersion: 'v1'
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('service-detail-page')).toBeInTheDocument();
    });
    expect(screen.getByText('Công ty 2')).toBeInTheDocument();
  });

  it('shows permission denied on 403', async () => {
    mockGetServiceById.mockRejectedValue({ response: { status: 403 } });
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    });
  });
});
