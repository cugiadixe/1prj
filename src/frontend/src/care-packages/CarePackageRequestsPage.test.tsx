import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CarePackageRequestsPage from './CarePackageRequestsPage';
import * as hooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./hooks');
vi.mock('../auth/AuthProvider');

const renderPage = () => {
  return render(
    <BrowserRouter>
      <CarePackageRequestsPage />
    </BrowserRouter>
  );
};

describe('CarePackageRequestsPage', () => {
  let mockHasPermission: any;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHasPermission = vi.fn().mockReturnValue(true);
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: mockHasPermission,
    });
  });

  it('renders loading state', () => {
    vi.spyOn(hooks, 'useCarePackageRequests').mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('care-package-list-loading')).toBeInTheDocument();
  });

  it('renders error state', () => {
    vi.spyOn(hooks, 'useCarePackageRequests').mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Network Error'),
    } as any);

    renderPage();
    expect(screen.getByTestId('care-package-list-error')).toBeInTheDocument();
  });

  it('renders empty state', () => {
    vi.spyOn(hooks, 'useCarePackageRequests').mockReturnValue({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('care-package-list-empty')).toBeInTheDocument();
  });

  it('renders list with data', () => {
    vi.spyOn(hooks, 'useCarePackageRequests').mockReturnValue({
      data: {
        items: [
          { id: 1, customerId: 10, status: 'Draft', totalAmount: 500000, saleDate: '2026-01-01', createdAt: '2026-01-01T00:00:00Z' },
        ],
        totalCount: 1,
      },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('care-package-list-table')).toBeInTheDocument();
    expect(screen.getByText('Nháp')).toBeInTheDocument();
  });

  it('hides create button if missing permission', () => {
    mockHasPermission.mockReturnValue(false);
    vi.spyOn(hooks, 'useCarePackageRequests').mockReturnValue({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.queryByTestId('create-care-package-btn')).not.toBeInTheDocument();
  });

  it('renders permission denied if API returns 403', () => {
    const error: any = new Error();
    error.isAxiosError = true;
    error.response = { status: 403 };

    vi.spyOn(hooks, 'useCarePackageRequests').mockReturnValue({
      data: undefined,
      isLoading: false,
      error,
    } as any);

    renderPage();
    expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
  });
});
