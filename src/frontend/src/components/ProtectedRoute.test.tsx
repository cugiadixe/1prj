import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider } from '../auth/AuthProvider';
import { clearAuthState, setAuthState } from '../auth/authState';
import * as authApi from '../auth/authApi';
import ProtectedRoute from './ProtectedRoute';

const makeWrapper =
  (initialPath = '/') =>
  ({ children }: { children: React.ReactNode }) => {
    const qc = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    return (
      <QueryClientProvider client={qc}>
        <MemoryRouter initialEntries={[initialPath]}>
          <AuthProvider>{children}</AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

describe('ProtectedRoute', () => {
  beforeEach(() => {
    clearAuthState();
    vi.restoreAllMocks();
  });

  it('redirects unauthenticated user to /login', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('No session'));

    render(
      <ProtectedRoute>
        <div data-testid="protected-content">Protected</div>
      </ProtectedRoute>,
      { wrapper: makeWrapper('/') },
    );

    // During bootstrap spinner should appear then disappear
    await waitFor(() =>
      expect(screen.queryByTestId('bootstrap-spinner')).toBeNull(),
    );

    // Protected content should NOT be visible
    expect(screen.queryByTestId('protected-content')).toBeNull();
  });

  it('renders content for authenticated user without mustChangePassword', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue({
      accessToken: 'tok',
      tokenType: 'Bearer',
      expiresIn: 900,
      expiresAtUtc: new Date().toISOString(),
      user: { userId: 1, username: 'u', displayName: null },
      mustChangePassword: false,
    });

    render(
      <ProtectedRoute>
        <div data-testid="protected-content">Protected</div>
      </ProtectedRoute>,
      { wrapper: makeWrapper('/') },
    );

    await waitFor(() =>
      expect(screen.getByTestId('protected-content')).toBeInTheDocument(),
    );
  });

  it('shows bootstrap spinner while resolving session', async () => {
    // Never resolves — stays in bootstrapping
    vi.spyOn(authApi, 'apiRefresh').mockImplementation(
      () => new Promise(() => {}),
    );

    render(
      <ProtectedRoute>
        <div data-testid="protected-content">Protected</div>
      </ProtectedRoute>,
      { wrapper: makeWrapper('/') },
    );

    expect(screen.getByTestId('bootstrap-spinner')).toBeInTheDocument();
    expect(screen.queryByTestId('protected-content')).toBeNull();
  });

  it('redirects authenticated+mustChangePassword user away from shell', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue({
      accessToken: 'tok',
      tokenType: 'Bearer',
      expiresIn: 900,
      expiresAtUtc: new Date().toISOString(),
      user: { userId: 1, username: 'u', displayName: null },
      mustChangePassword: true,
    });

    render(
      <ProtectedRoute>
        <div data-testid="protected-content">Protected</div>
      </ProtectedRoute>,
      { wrapper: makeWrapper('/') },
    );

    await waitFor(() =>
      expect(screen.queryByTestId('protected-content')).toBeNull(),
    );
  });

  it('does not expose protected content to unauthenticated user', async () => {
    vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('Unauthorized'));

    render(
      <ProtectedRoute>
        <div data-testid="protected-content">SECRET</div>
      </ProtectedRoute>,
      { wrapper: makeWrapper('/admin') },
    );

    await waitFor(() =>
      expect(screen.queryByTestId('bootstrap-spinner')).toBeNull(),
    );

    expect(screen.queryByText('SECRET')).toBeNull();
  });

  it('renders content when pre-seeded in-memory state is confirmed by refresh', async () => {
    setAuthState('pre-seeded-tok', false, null);
    vi.spyOn(authApi, 'apiRefresh').mockResolvedValue({
      accessToken: 'refreshed-tok',
      tokenType: 'Bearer',
      expiresIn: 900,
      expiresAtUtc: new Date().toISOString(),
      user: { userId: 1, username: 'u', displayName: null },
      mustChangePassword: false,
    });

    render(
      <ProtectedRoute>
        <div data-testid="protected-content">Protected</div>
      </ProtectedRoute>,
      { wrapper: makeWrapper('/') },
    );

    await waitFor(() =>
      expect(screen.getByTestId('protected-content')).toBeInTheDocument(),
    );
  });
});
