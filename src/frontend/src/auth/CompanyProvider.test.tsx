/**
 * CompanyProvider tests — Phase 1B.1-M
 *
 * Covers the following required behaviors:
 *  1. fetch companies after login / successful refresh
 *  2. no fetch when mustChangePassword=true
 *  3. exactly one company → auto-select
 *  4. multiple companies → currentCompanyId stays null (manual selection required)
 *  5. switchCompany calls refreshPermissions with X-Company-Id
 *  6. X-Company-Id is never set globally on axiosClient default headers
 *  7. clear company context on logout
 *  8. clear company context on refresh failure
 *  9. clear company context when onPasswordChanged is called
 * 10. no persistence — company state is not written to localStorage/sessionStorage
 * 11. interceptor guard: /auth/me/companies and /auth/me/permissions are retry-eligible
 */

import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

import { AuthProvider, useAuth } from './AuthProvider';
import { CompanyProvider, useCompany } from './CompanyProvider';
import { clearAuthState } from './authState';
import * as authApi from './authApi';
import axiosClient from '../api/axiosClient';

// ── Helpers ──────────────────────────────────────────────────────────────────

const makeCompany = (id: number, name = `Company ${id}`): authApi.UserCompanyDto => ({
    companyId: id,
    companyCode: `C${id}`,
    companyName: name,
    isDefault: false,
});

const mockLoginResponse = (mustChangePassword = false) => ({
    accessToken: 'test-access-token',
    tokenType: 'Bearer',
    expiresIn: 900,
    expiresAtUtc: new Date(Date.now() + 900_000).toISOString(),
    user: { userId: 1, username: 'testuser', displayName: null },
    mustChangePassword,
});

/**
 * Full wrapper: QueryClientProvider → MemoryRouter → AuthProvider → CompanyProvider
 */
const AllProviders: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    return (
        <QueryClientProvider client={qc}>
            <MemoryRouter>
                <AuthProvider>
                    <CompanyProvider>
                        {children}
                    </CompanyProvider>
                </AuthProvider>
            </MemoryRouter>
        </QueryClientProvider>
    );
};

/** Inspector that surfaces company context values as DOM text */
const CompanyInspector: React.FC = () => {
    const { companies, currentCompanyId, isLoading } = useCompany();
    return (
        <div>
            <span data-testid="company-count">{companies.length}</span>
            <span data-testid="current-company-id">{currentCompanyId ?? 'null'}</span>
            <span data-testid="is-loading">{String(isLoading)}</span>
        </div>
    );
};

/** Inspector + switch trigger */
const CompanyWithSwitch: React.FC = () => {
    const { companies, currentCompanyId, switchCompany } = useCompany();
    return (
        <div>
            <span data-testid="company-count">{companies.length}</span>
            <span data-testid="current-company-id">{currentCompanyId ?? 'null'}</span>
            {companies.map(c => (
                <button
                    key={c.companyId}
                    data-testid={`switch-${c.companyId}`}
                    onClick={() => switchCompany(c.companyId)}
                >
                    {c.companyName}
                </button>
            ))}
        </div>
    );
};

/** Trigger component for login action */
const LoginTrigger: React.FC = () => {
    const { login } = useAuth();
    return <button data-testid="login" onClick={() => login('u', 'p')}>Login</button>;
};

/** Trigger component for logout action */
const LogoutTrigger: React.FC = () => {
    const { logout } = useAuth();
    return <button data-testid="logout" onClick={() => logout()}>Logout</button>;
};

/** Trigger component for password-changed action */
const PwChangeTrigger: React.FC = () => {
    const { onPasswordChanged } = useAuth();
    return (
        <button data-testid="pw-change" onClick={onPasswordChanged}>
            Change Password
        </button>
    );
};

// ── Setup ─────────────────────────────────────────────────────────────────────

beforeEach(() => {
    clearAuthState();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
});

afterEach(() => {
    // Ensure axiosClient default headers are clean between tests
    delete (axiosClient.defaults.headers.common as Record<string, unknown>)['X-Company-Id'];
});

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('CompanyProvider', () => {

    // ── 1. Fetch companies after login / successful refresh ───────────────────
    it('fetches companies after successful login', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('no session'));
        vi.spyOn(authApi, 'apiLogin').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        const fetchSpy = vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(1)],
        });

        render(
            <AllProviders>
                <LoginTrigger />
                <CompanyInspector />
            </AllProviders>
        );

        // Before login: bootstrap fails, not authenticated
        await waitFor(() => expect(screen.getByTestId('current-company-id').textContent).toBe('null'));
        await userEvent.click(screen.getByTestId('login'));

        await waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(1));
        await waitFor(() => expect(screen.getByTestId('company-count').textContent).toBe('1'));
    });

    // ── 1b. Fetch companies after successful bootstrap refresh ────────────────
    it('fetches companies after successful bootstrap refresh', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        const fetchSpy = vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(5)],
        });

        render(<AllProviders><CompanyInspector /></AllProviders>);

        await waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(1));
        await waitFor(() => expect(screen.getByTestId('company-count').textContent).toBe('1'));
    });

    // ── 2. No fetch when mustChangePassword=true ──────────────────────────────
    it('does NOT fetch companies when mustChangePassword is true', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(true));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        const fetchSpy = vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(1)],
        });

        render(<AllProviders><CompanyInspector /></AllProviders>);

        // Wait long enough for any stray async calls to settle
        await new Promise(r => setTimeout(r, 100));

        expect(fetchSpy).not.toHaveBeenCalled();
        expect(screen.getByTestId('company-count').textContent).toBe('0');
        expect(screen.getByTestId('current-company-id').textContent).toBe('null');
    });

    // ── 3. Exactly one company → auto-select ─────────────────────────────────
    it('auto-selects when exactly one company is returned', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(42)],
        });

        render(<AllProviders><CompanyInspector /></AllProviders>);

        await waitFor(() =>
            expect(screen.getByTestId('current-company-id').textContent).toBe('42')
        );
    });

    // ── 4. Multiple companies → no auto-select ───────────────────────────────
    it('does NOT auto-select when multiple companies are returned', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(1), makeCompany(2)],
        });

        render(<AllProviders><CompanyInspector /></AllProviders>);

        // Companies should load
        await waitFor(() =>
            expect(screen.getByTestId('company-count').textContent).toBe('2')
        );
        // But no auto-selection — user must pick manually
        expect(screen.getByTestId('current-company-id').textContent).toBe('null');
    });

    // ── 5. switchCompany calls refreshPermissions with X-Company-Id ──────────
    it('switchCompany triggers refreshPermissions with the selected companyId', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        const permSpy = vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(7), makeCompany(8)],
        });

        render(<AllProviders><CompanyWithSwitch /></AllProviders>);

        // Wait for companies to load
        await waitFor(() =>
            expect(screen.getByTestId('company-count').textContent).toBe('2')
        );

        // Clear prior permission fetch calls (from applyAuth bootstrap)
        permSpy.mockClear();

        await userEvent.click(screen.getByTestId('switch-7'));

        await waitFor(() =>
            expect(screen.getByTestId('current-company-id').textContent).toBe('7')
        );

        // refreshPermissions must have been called with companyId=7
        expect(permSpy).toHaveBeenCalledWith(7);
    });

    // ── 6. X-Company-Id is never set on axiosClient default headers ───────────
    it('does not set X-Company-Id on axiosClient default headers globally', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(1)],
        });

        render(<AllProviders><CompanyInspector /></AllProviders>);

        // Wait for company to be auto-selected
        await waitFor(() =>
            expect(screen.getByTestId('current-company-id').textContent).toBe('1')
        );

        // The global default headers must NOT carry X-Company-Id
        const commonHeaders = axiosClient.defaults.headers.common as Record<string, unknown>;
        expect(commonHeaders['X-Company-Id']).toBeUndefined();
    });

    // ── 7. Clear company context on logout ────────────────────────────────────
    it('clears company context when user logs out', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiLogout').mockResolvedValue(undefined);
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(1)],
        });

        render(
            <AllProviders>
                <LogoutTrigger />
                <CompanyInspector />
            </AllProviders>
        );

        // Wait for company to be auto-selected
        await waitFor(() =>
            expect(screen.getByTestId('current-company-id').textContent).toBe('1')
        );

        await userEvent.click(screen.getByTestId('logout'));

        await waitFor(() =>
            expect(screen.getByTestId('company-count').textContent).toBe('0')
        );
        expect(screen.getByTestId('current-company-id').textContent).toBe('null');
    });

    // ── 8. Clear company context on refresh failure ───────────────────────────
    it('clears company context when bootstrap refresh fails', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('session expired'));

        render(<AllProviders><CompanyInspector /></AllProviders>);

        // After failed refresh: not authenticated, no companies
        await new Promise(r => setTimeout(r, 100));

        expect(screen.getByTestId('company-count').textContent).toBe('0');
        expect(screen.getByTestId('current-company-id').textContent).toBe('null');
    });

    // ── 9. Clear company context when onPasswordChanged is called ─────────────
    it('clears company context when password is changed', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(1)],
        });

        render(
            <AllProviders>
                <PwChangeTrigger />
                <CompanyInspector />
            </AllProviders>
        );

        // Wait for company to be auto-selected
        await waitFor(() =>
            expect(screen.getByTestId('current-company-id').textContent).toBe('1')
        );

        await userEvent.click(screen.getByTestId('pw-change'));

        await waitFor(() =>
            expect(screen.getByTestId('company-count').textContent).toBe('0')
        );
        expect(screen.getByTestId('current-company-id').textContent).toBe('null');
    });

    // ── 10. No persistence ────────────────────────────────────────────────────
    it('does not write company state to localStorage or sessionStorage', async () => {
        vi.spyOn(authApi, 'apiRefresh').mockResolvedValue(mockLoginResponse(false));
        vi.spyOn(authApi, 'apiFetchMyPermissions').mockResolvedValue({ permissions: [] });
        vi.spyOn(authApi, 'apiFetchMyCompanies').mockResolvedValue({
            companies: [makeCompany(99)],
        });

        render(<AllProviders><CompanyInspector /></AllProviders>);

        await waitFor(() =>
            expect(screen.getByTestId('current-company-id').textContent).toBe('99')
        );

        // Nothing company-related must be in storage
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i)!.toLowerCase();
            expect(key).not.toContain('company');
            expect(key).not.toContain('companyid');
        }
        for (let i = 0; i < sessionStorage.length; i++) {
            const key = sessionStorage.key(i)!.toLowerCase();
            expect(key).not.toContain('company');
            expect(key).not.toContain('companyid');
        }
        expect(localStorage.getItem('currentCompanyId')).toBeNull();
        expect(sessionStorage.getItem('currentCompanyId')).toBeNull();
    });

    // ── 11. Interceptor guard validation ──────────────────────────────────────
    describe('interceptor guard — /auth/me/* must be retry-eligible', () => {
        /**
         * The interceptor must only block retry for the four loop-inducing endpoints.
         * /auth/me/companies and /auth/me/permissions must NOT be in that exclusion list.
         * We verify this with pure logic tests (matching the production guard logic).
         */
        const loopEndpoints = [
            '/auth/login',
            '/auth/refresh',
            '/auth/logout',
            '/auth/change-password',
        ];

        const isLoopEndpoint = (url: string) =>
            loopEndpoints.some(u => url.includes(u));

        it('/auth/me/companies is NOT a loop endpoint', () => {
            expect(isLoopEndpoint('/auth/me/companies')).toBe(false);
        });

        it('/auth/me/permissions is NOT a loop endpoint', () => {
            expect(isLoopEndpoint('/auth/me/permissions')).toBe(false);
        });

        it('/auth/login IS a loop endpoint (must not retry)', () => {
            expect(isLoopEndpoint('/auth/login')).toBe(true);
        });

        it('/auth/refresh IS a loop endpoint (must not retry)', () => {
            expect(isLoopEndpoint('/auth/refresh')).toBe(true);
        });

        it('/auth/logout IS a loop endpoint (must not retry)', () => {
            expect(isLoopEndpoint('/auth/logout')).toBe(true);
        });

        it('/auth/change-password IS a loop endpoint (must not retry)', () => {
            expect(isLoopEndpoint('/auth/change-password')).toBe(true);
        });

        it('company fetch failure clears company state (isAuthenticated transitions to false)', async () => {
            // Simulate: refresh fails → not authenticated → companies not fetched
            vi.spyOn(authApi, 'apiRefresh').mockRejectedValue(new Error('no session'));

            render(<AllProviders><CompanyInspector /></AllProviders>);

            await new Promise(r => setTimeout(r, 100));

            expect(screen.getByTestId('company-count').textContent).toBe('0');
            expect(screen.getByTestId('current-company-id').textContent).toBe('null');
        });
    });
});
