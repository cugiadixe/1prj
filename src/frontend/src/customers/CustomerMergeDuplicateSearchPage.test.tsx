import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import CustomerMergeDuplicateSearchPage from './CustomerMergeDuplicateSearchPage';
import { findMergeDuplicates } from './customerMergeApi';

vi.mock('./customerMergeApi', () => ({
  findMergeDuplicates: vi.fn(),
}));

describe('CustomerMergeDuplicateSearchPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.resetAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const renderPage = () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CustomerMergeDuplicateSearchPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );
  };

  it('renders search form', () => {
    renderPage();
    expect(
      screen.getByText('Tìm khách hàng trùng lặp'),
    ).toBeInTheDocument();
    expect(screen.getByTestId('duplicate-search-form')).toBeInTheDocument();
  });

  it('shows error when submitting empty form', async () => {
    renderPage();
    fireEvent.click(screen.getByTestId('search-duplicates-button'));

    await waitFor(() => {
      expect(screen.getByTestId('search-error')).toBeInTheDocument();
    });
  });

  it('shows no duplicates message when none found', async () => {
    vi.mocked(findMergeDuplicates).mockResolvedValueOnce({
      hasDuplicates: false,
      matches: [],
    });

    renderPage();
    fireEvent.change(screen.getByTestId('input-search-cccd'), {
      target: { value: '123456789012' },
    });
    fireEvent.click(screen.getByTestId('search-duplicates-button'));

    await waitFor(() => {
      expect(
        screen.getByTestId('no-duplicates-message'),
      ).toBeInTheDocument();
    });
  });

  it('shows search error on API failure', async () => {
    vi.mocked(findMergeDuplicates).mockRejectedValueOnce({
      response: { status: 403 },
    });

    renderPage();
    fireEvent.change(screen.getByTestId('input-search-cccd'), {
      target: { value: '123' },
    });
    fireEvent.click(screen.getByTestId('search-duplicates-button'));

    await waitFor(() => {
      expect(screen.getByTestId('search-error')).toBeInTheDocument();
    });
  });

  it('displays duplicate results table', async () => {
    vi.mocked(findMergeDuplicates).mockResolvedValueOnce({
      hasDuplicates: true,
      matches: [
        {
          id: 1,
          customerCode: 'C001',
          fullName: 'Test Customer',
          cccd: '123456789012',
          phone: null,
          customerStatus: 'ACTIVE',
          createdAt: '2026-01-01T00:00:00Z',
        },
      ],
    });

    renderPage();
    fireEvent.change(screen.getByTestId('input-search-cccd'), {
      target: { value: '123456789012' },
    });
    fireEvent.click(screen.getByTestId('search-duplicates-button'));

    await waitFor(() => {
      expect(
        screen.getByTestId('duplicate-results-table'),
      ).toBeInTheDocument();
      expect(screen.getByText('Test Customer')).toBeInTheDocument();
      expect(screen.getByText('C001')).toBeInTheDocument();
    });
  });
});
