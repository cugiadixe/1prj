import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import PermissionAssignmentPage from './PermissionAssignmentPage';
import * as api from './permissionAssignmentApi';
import * as accountApi from '../accountManagement/accountManagementApi';
import { useCompany } from '../auth/CompanyProvider';

vi.mock('./permissionAssignmentApi', () => ({
  fetchPermissionCatalog: vi.fn(),
  fetchUserIndividualPermissions: vi.fn(),
  fetchEffectivePermissions: vi.fn(),
  grantIndividualPermission: vi.fn(),
  deactivateIndividualPermission: vi.fn(),
}));

vi.mock('../accountManagement/accountManagementApi', () => ({
  searchAccounts: vi.fn(),
}));

vi.mock('../auth/CompanyProvider', () => ({
  useCompany: vi.fn(),
}));

const MOCK_CATALOG = [
  { permissionCode: 'SECURITY_ADMIN_MANAGE', moduleCode: 'SECURITY', actionCode: 'ADMIN_MANAGE', dataScope: 'GLOBAL', isSensitive: true, isDelegable: false, requiresReason: true, isActive: true, description: 'Manage security' },
];

const MOCK_ASSIGNMENTS = [
  { id: 1, userId: 100, permissionCode: 'SECURITY_ADMIN_MANAGE', scopeType: 'GLOBAL', companyId: null, grantType: 'ALLOW', assignmentStatus: 'ACTIVE', effectiveFrom: '2026-01-01T00:00:00Z', effectiveTo: null, reason: 'test', rowVersion: 'v1' },
];

const MOCK_EFFECTIVE = {
  userId: 100,
  companyId: null,
  permissionCodes: ['SECURITY_ADMIN_MANAGE'],
};

const MOCK_ACCOUNTS = {
  items: [
    { accountId: 42, userId: 100, username: 'alice', fullName: 'Alice', employeeCode: 'EMP1', providerType: 'INTERNAL', status: 'ACTIVE', employmentStatus: 'ACTIVE' },
  ],
  totalCount: 1,
};

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/security/permissions/assignments']}>
        {children}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('PermissionAssignmentPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (useCompany as ReturnType<typeof vi.fn>).mockReturnValue({
      currentCompanyId: null,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders page and loads catalog', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);

    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    expect(screen.getByTestId('permission-assignment-loading')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId('permission-assignment-page')).toBeInTheDocument();
    });
    expect(screen.getByText('Permission Assignment')).toBeInTheDocument();
  });

  it('searches for a user', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (accountApi.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACCOUNTS);

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByTestId('user-search-input')).toBeInTheDocument();
    });

    const input = screen.getByPlaceholderText(/Search by username/i);
    await user.type(input, 'alice{Enter}');

    await waitFor(() => {
      expect(accountApi.searchAccounts).toHaveBeenCalledWith({ search: 'alice', page: 1, pageSize: 20 });
    });
    expect(screen.getByText('alice — Alice')).toBeInTheDocument();
  });

  it('selects a user and loads assignments', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (accountApi.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACCOUNTS);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ASSIGNMENTS);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_EFFECTIVE);

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-search-input')).toBeInTheDocument());
    await user.type(screen.getByPlaceholderText(/Search by username/i), 'alice{Enter}');

    await waitFor(() => expect(screen.getByTestId('select-user-100')).toBeInTheDocument());
    await user.click(screen.getByTestId('select-user-100'));

    await waitFor(() => {
      expect(screen.getByTestId('assignments-card')).toBeInTheDocument();
      expect(screen.getByTestId('effective-permissions-card')).toBeInTheDocument();
    });

    expect(screen.getAllByText('SECURITY_ADMIN_MANAGE').length).toBeGreaterThan(0);
    expect(screen.getByText('ALLOW')).toBeInTheDocument();
    expect(screen.getByText('GLOBAL')).toBeInTheDocument();
  });

  it('opens grant modal and requires reason if validation is hit', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (accountApi.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACCOUNTS);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue({ userId: 100, companyId: null, permissionCodes: [] });

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-search-input')).toBeInTheDocument());
    await user.type(screen.getByPlaceholderText(/Search by username/i), 'alice{Enter}');
    await waitFor(() => expect(screen.getByTestId('select-user-100')).toBeInTheDocument());
    await user.click(screen.getByTestId('select-user-100'));

    await waitFor(() => expect(screen.getByTestId('grant-permission-button')).toBeInTheDocument());
    await user.click(screen.getByTestId('grant-permission-button'));

    expect(screen.getByTestId('grant-permission-modal')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Grant' }));

    expect(screen.getByTestId('grant-validation-error')).toHaveTextContent('Please select a permission');
  });

  it('submits grant request', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (accountApi.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACCOUNTS);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue({ userId: 100, companyId: null, permissionCodes: [] });
    (api.grantIndividualPermission as ReturnType<typeof vi.fn>).mockResolvedValue({ ...MOCK_ASSIGNMENTS[0] });

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-search-input')).toBeInTheDocument());
    await user.type(screen.getByPlaceholderText(/Search by username/i), 'alice{Enter}');
    await waitFor(() => expect(screen.getByTestId('select-user-100')).toBeInTheDocument());
    await user.click(screen.getByTestId('select-user-100'));

    await waitFor(() => expect(screen.getByTestId('grant-permission-button')).toBeInTheDocument());
    await user.click(screen.getByTestId('grant-permission-button'));

    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Select permission' })).toBeInTheDocument());
    await user.click(screen.getByRole('combobox', { name: 'Select permission' }));

    // Select permission
    await user.click(screen.getByText('SECURITY_ADMIN_MANAGE — Manage security'));

    // Submit
    await user.click(screen.getByRole('button', { name: 'Grant' }));

    await waitFor(() => {
      expect(api.grantIndividualPermission).toHaveBeenCalledWith(100, expect.objectContaining({
        permissionCode: 'SECURITY_ADMIN_MANAGE',
        scopeType: 'GLOBAL',
        grantType: 'ALLOW'
      }));
    });

    await waitFor(() => expect(screen.getByTestId('success-message')).toBeInTheDocument());
  });
});
