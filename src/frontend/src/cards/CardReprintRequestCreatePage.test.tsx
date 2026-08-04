import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import CardReprintRequestCreatePage from './CardReprintRequestCreatePage';
import * as hooks from './hooks';
import * as auth from '../auth/AuthProvider';

vi.mock('./hooks');
vi.mock('../auth/AuthProvider');

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const renderPage = () => {
  return render(
    <BrowserRouter>
      <CardReprintRequestCreatePage />
    </BrowserRouter>
  );
};

describe('CardReprintRequestCreatePage', () => {
  let mockHasPermission: any;
  let mutateAsync: any;

  beforeEach(() => {
    vi.resetAllMocks();
    mockHasPermission = vi.fn().mockReturnValue(true);
    vi.spyOn(auth, 'usePermissions').mockReturnValue({
      permissions: [],
      hasPermission: mockHasPermission,
    });

    mutateAsync = vi.fn();
    vi.spyOn(hooks, 'useCreateCardReprintRequest').mockReturnValue({
      mutateAsync,
      isPending: false,
    } as any);
  });

  it('renders permission denied if missing permission', () => {
    mockHasPermission.mockReturnValue(false);
    renderPage();
    expect(screen.getByTestId('permission-denied')).toBeInTheDocument();
  });

  it('renders create form', () => {
    renderPage();
    expect(screen.getByTestId('card-reprint-create-form')).toBeInTheDocument();
  });

  it('submits form successfully and navigates', async () => {
    mutateAsync.mockResolvedValue({ id: 99 });
    renderPage();

    fireEvent.change(screen.getByTestId('input-cardId'), { target: { value: '123' } });
    fireEvent.click(screen.getByTestId('submit-btn'));

    await waitFor(() => {
      expect(mutateAsync).toHaveBeenCalled();
      expect(mockNavigate).toHaveBeenCalledWith('/cards/reprints/99');
    });
  });

  it('displays error on API failure', async () => {
    mutateAsync.mockRejectedValue(new Error('Failed to create'));
    renderPage();

    fireEvent.change(screen.getByTestId('input-cardId'), { target: { value: '123' } });
    fireEvent.click(screen.getByTestId('submit-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('create-error')).toBeInTheDocument();
      expect(screen.getByText('Failed to create')).toBeInTheDocument();
    });
  });
});
