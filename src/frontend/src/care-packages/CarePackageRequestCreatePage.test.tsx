import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CarePackageRequestCreatePage from './CarePackageRequestCreatePage';
import * as hooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./hooks');
vi.mock('../auth/AuthProvider');
vi.mock('../customers/customersApi', () => ({
  searchCustomers: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
}));
vi.mock('../services/serviceTypesApi', () => ({
  searchServiceTypes: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
}));
vi.mock('../graves/gravesApi', () => ({
  searchGraves: vi.fn().mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
}));

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

const renderPage = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <CarePackageRequestCreatePage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('CarePackageRequestCreatePage', () => {
  let mockHasPermission: any;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHasPermission = vi.fn().mockReturnValue(true);
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: mockHasPermission,
    });
    vi.spyOn(hooks, 'useCreateCarePackageRequest').mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
    } as any);
  });

  it('renders create form', () => {
    renderPage();
    expect(screen.getByTestId('care-package-create-page')).toBeInTheDocument();
    expect(screen.getByTestId('care-package-create-form')).toBeInTheDocument();
  });

  it('renders permission denied if missing CARE_PACKAGE_CREATE', () => {
    mockHasPermission.mockReturnValue(false);
    renderPage();
    expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
    expect(screen.queryByTestId('care-package-create-form')).not.toBeInTheDocument();
  });

  it('renders form fields', () => {
    renderPage();
    expect(screen.getByTestId('input-customerId')).toBeInTheDocument();
    expect(screen.getByTestId('input-cotCount')).toBeInTheDocument();
    expect(screen.getByTestId('submit-btn')).toBeInTheDocument();
    expect(screen.getByTestId('cancel-btn')).toBeInTheDocument();
  });
});
