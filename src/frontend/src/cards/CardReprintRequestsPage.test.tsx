import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CardReprintRequestsPage from './CardReprintRequestsPage';
import * as hooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./hooks');
vi.mock('../auth/AuthProvider');

const renderPage = () => {
  return render(
    <BrowserRouter>
      <CardReprintRequestsPage />
    </BrowserRouter>
  );
};

describe('CardReprintRequestsPage', () => {
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
    vi.spyOn(hooks, 'useCardReprintRequests').mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('card-reprint-list-loading')).toBeInTheDocument();
  });

  it('renders error state', () => {
    vi.spyOn(hooks, 'useCardReprintRequests').mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Network Error'),
    } as any);

    renderPage();
    expect(screen.getByTestId('card-reprint-list-error')).toBeInTheDocument();
  });

  it('renders empty state', () => {
    vi.spyOn(hooks, 'useCardReprintRequests').mockReturnValue({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('card-reprint-list-empty')).toBeInTheDocument();
  });

  it('renders list with data', () => {
    vi.spyOn(hooks, 'useCardReprintRequests').mockReturnValue({
      data: {
        items: [
          { id: 1, cardId: 100, status: 'DRAFT', createdAt: '2026-01-01T00:00:00Z' },
        ],
        totalCount: 1,
      },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('card-reprint-list-table')).toBeInTheDocument();
    expect(screen.getByText('DRAFT')).toBeInTheDocument();
  });

  it('hides create button if missing permission', () => {
    mockHasPermission.mockReturnValue(false);
    vi.spyOn(hooks, 'useCardReprintRequests').mockReturnValue({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.queryByTestId('create-card-reprint-btn')).not.toBeInTheDocument();
  });

  it('renders permission denied if API returns 403', () => {
    const error: any = new Error();
    error.isAxiosError = true;
    error.response = { status: 403 };

    vi.spyOn(hooks, 'useCardReprintRequests').mockReturnValue({
      data: undefined,
      isLoading: false,
      error,
    } as any);

    renderPage();
    expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
  });
});
