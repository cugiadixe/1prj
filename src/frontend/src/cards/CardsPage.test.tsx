import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CardsPage from './CardsPage';
import * as cardsHooks from './cardsHooks';
import * as reprintHooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./cardsHooks');
vi.mock('./hooks');
vi.mock('../auth/AuthProvider');

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

const renderPage = () => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <BrowserRouter>
        <CardsPage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('CardsPage', () => {
  let createReprintAsync: any;
  let printInitialAsync: any;

  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: vi.fn().mockReturnValue(true),
    } as any);

    createReprintAsync = vi.fn().mockResolvedValue({ id: 99 });
    printInitialAsync = vi.fn().mockResolvedValue({ id: 99 });

    vi.spyOn(cardsHooks, 'useCreateCard').mockReturnValue({ mutateAsync: vi.fn(), isPending: false } as any);
    vi.spyOn(reprintHooks, 'useCreateCardReprintRequest').mockReturnValue({ mutateAsync: createReprintAsync, isPending: false } as any);
    vi.spyOn(reprintHooks, 'usePrintInitialCardReprint').mockReturnValue({ mutateAsync: printInitialAsync, isPending: false } as any);
  });

  it('shows print-initial action for a never-printed card', () => {
    vi.spyOn(cardsHooks, 'useCards').mockReturnValue({
      data: [{ id: 1, companyId: 1, graveId: 'A-01', cardNumber: '1', serviceId: null, printCount: 0, status: 'ACTIVE', createdAt: '' }],
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-issue-card')).toBeInTheDocument();
    expect(screen.getByTestId('btn-print-initial-1')).toBeInTheDocument();
    expect(screen.queryByTestId('btn-request-reprint-1')).not.toBeInTheDocument();
  });

  it('shows request-reprint action for an already-printed card', () => {
    vi.spyOn(cardsHooks, 'useCards').mockReturnValue({
      data: [{ id: 2, companyId: 1, graveId: 'A-02', cardNumber: '2', serviceId: null, printCount: 1, status: 'ACTIVE', createdAt: '' }],
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    expect(screen.getByTestId('btn-request-reprint-2')).toBeInTheDocument();
    expect(screen.queryByTestId('btn-print-initial-2')).not.toBeInTheDocument();
  });

  it('creating a reprint navigates to its detail page', async () => {
    vi.spyOn(cardsHooks, 'useCards').mockReturnValue({
      data: [{ id: 2, companyId: 1, graveId: 'A-02', cardNumber: '2', serviceId: null, printCount: 1, status: 'ACTIVE', createdAt: '' }],
      isLoading: false,
      error: null,
    } as any);

    renderPage();
    fireEvent.click(screen.getByTestId('btn-request-reprint-2'));

    await waitFor(() => {
      expect(createReprintAsync).toHaveBeenCalledWith({ cardId: 2 });
      expect(mockNavigate).toHaveBeenCalledWith('/cards/reprints/99');
    });
  });
});
