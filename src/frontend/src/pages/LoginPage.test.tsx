import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider } from '../auth/AuthProvider';
import { clearAuthState } from '../auth/authState';
import * as authApi from '../auth/authApi';
import LoginPage from './LoginPage';

const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/login']}>
        <AuthProvider>{children}</AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('LoginPage', () => {
  beforeEach(() => {
    clearAuthState();
    vi.restoreAllMocks();
  });

  it('renders login form', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));

    render(<LoginPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId('login-submit')).toBeInTheDocument(),
    );
    expect(screen.getByTestId('login-username')).toBeInTheDocument();
    expect(screen.getByTestId('login-password')).toBeInTheDocument();
  });

  it('validates required fields', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));

    render(<LoginPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId('login-submit')).toBeInTheDocument(),
    );

    await userEvent.click(screen.getByTestId('login-submit'));

    await waitFor(() => {
      expect(
        screen.getByText('Vui lòng nhập tên đăng nhập.'),
      ).toBeInTheDocument();
    });
  });

  it('displays sanitized error on login failure', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));
    vi.spyOn(authApi, 'apiLogin').mockRejectedValue({
      response: { status: 401 },
    });

    render(<LoginPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId('login-submit')).toBeInTheDocument(),
    );

    await userEvent.type(screen.getByTestId('login-username'), 'baduser');
    await userEvent.type(screen.getByTestId('login-password'), 'badpass');
    await userEvent.click(screen.getByTestId('login-submit'));

    await waitFor(() =>
      expect(screen.getByTestId('login-error')).toBeInTheDocument(),
    );

    const errorEl = screen.getByTestId('login-error');
    expect(errorEl.textContent).not.toContain('stack');
    expect(errorEl.textContent).not.toContain('Exception');
  });

  it('does not display raw backend error detail', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));
    vi.spyOn(authApi, 'apiLogin').mockRejectedValue({
      response: {
        status: 401,
        data: { detail: 'INTERNAL_STACK_TRACE_HERE' },
      },
    });

    render(<LoginPage />, { wrapper: Wrapper });

    await waitFor(() =>
      expect(screen.getByTestId('login-submit')).toBeInTheDocument(),
    );

    await userEvent.type(screen.getByTestId('login-username'), 'u');
    await userEvent.type(screen.getByTestId('login-password'), 'p');
    await userEvent.click(screen.getByTestId('login-submit'));

    await waitFor(() =>
      expect(screen.getByTestId('login-error')).toBeInTheDocument(),
    );

    expect(screen.queryByText('INTERNAL_STACK_TRACE_HERE')).toBeNull();
  });
});
