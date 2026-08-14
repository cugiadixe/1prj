import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import EffectivePermissionDiagnosticsPage from './EffectivePermissionDiagnosticsPage';
import { effectivePermissionDiagnosticsApi } from './effectivePermissionDiagnosticsApi';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';

vi.mock('./effectivePermissionDiagnosticsApi');
const mockedApi = effectivePermissionDiagnosticsApi as any;

let mockCompanyId: number | null = null;
vi.mock('../auth/CompanyProvider', async () => {
  const actual = await vi.importActual('../auth/CompanyProvider');
  return {
    ...(actual as any),
    useCompany: () => ({
      currentCompanyId: mockCompanyId,
      companies: [],
      switchCompany: vi.fn(),
    }),
  };
});

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const renderComponent = () => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <EffectivePermissionDiagnosticsPage />
      </BrowserRouter>
    </QueryClientProvider>
  );
};

const MOCK_CATALOG = [
  {
    permissionCode: 'SECURITY_ADMIN_MANAGE',
    moduleCode: 'SECURITY',
    actionCode: 'MANAGE',
    dataScope: 'GLOBAL',
    isSensitive: false,
    isDelegable: false,
    requiresReason: false,
    isActive: true,
    description: 'Manage security administration',
  },
  {
    permissionCode: 'ORGANIZATION_USER_MANAGE',
    moduleCode: 'ORGANIZATION',
    actionCode: 'MANAGE',
    dataScope: 'GLOBAL',
    isSensitive: false,
    isDelegable: false,
    requiresReason: false,
    isActive: true,
    description: 'Manage organization users',
  },
];

const MOCK_EFFECTIVE = {
  userId: 42,
  companyId: null,
  permissionCodes: ['SECURITY_ADMIN_MANAGE', 'ORGANIZATION_USER_MANAGE'],
};

const MOCK_INDIVIDUAL = [
  {
    id: 1,
    userId: 42,
    permissionCode: 'SECURITY_ADMIN_MANAGE',
    scopeType: 'GLOBAL',
    companyId: null,
    grantType: 'ALLOW',
    assignmentStatus: 'Active',
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: null,
    reason: null,
    rowVersion: 'rv1',
  },
];

const MOCK_ROLES = [
  {
    id: 10,
    userId: 42,
    roleId: 5,
    roleCode: 'ADMIN_ROLE',
    roleName: 'Administrator',
    scopeType: 'GLOBAL',
    companyId: null,
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: null,
    isActive: true,
    rowVersion: 'rv2',
  },
];

const MOCK_ADMIN_GROUPS = [
  {
    id: 20,
    userId: 42,
    adminGroupId: 3,
    groupCode: 'SEC_GROUP',
    groupName: 'Security Group',
    assignmentStatus: 'Active',
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: null,
    rowVersion: 'rv3',
  },
];

describe('EffectivePermissionDiagnosticsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockCompanyId = null;
    queryClient.clear();

    mockedApi.fetchPermissionCatalog.mockResolvedValue(MOCK_CATALOG);
    mockedApi.fetchEffectivePermissions.mockResolvedValue(MOCK_EFFECTIVE);
    mockedApi.fetchUserIndividualPermissions.mockResolvedValue(MOCK_INDIVIDUAL);
    mockedApi.fetchUserRoleAssignments.mockResolvedValue(MOCK_ROLES);
    mockedApi.fetchUserAdminGroupAssignments.mockResolvedValue(MOCK_ADMIN_GROUPS);
  });

  it('renders the page with user ID input', () => {
    renderComponent();
    expect(screen.getByTestId('effective-permission-diagnostics-page')).toBeInTheDocument();
    expect(screen.getByTestId('user-id-input')).toBeInTheDocument();
    expect(screen.getByTestId('lookup-button')).toBeInTheDocument();
  });

  it('validates missing user ID', () => {
    renderComponent();
    fireEvent.click(screen.getByTestId('lookup-button'));
    expect(screen.getByTestId('validation-error')).toBeInTheDocument();
    expect(screen.getByText('Mã người dùng là bắt buộc.')).toBeInTheDocument();
  });

  it('validates non-integer user ID', () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: 'abc' } });
    fireEvent.click(screen.getByTestId('lookup-button'));
    expect(screen.getByTestId('validation-error')).toBeInTheDocument();
    expect(screen.getByText('Mã người dùng phải là số nguyên dương.')).toBeInTheDocument();
  });

  it('validates negative user ID', () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '-5' } });
    fireEvent.click(screen.getByTestId('lookup-button'));
    expect(screen.getByText('Mã người dùng phải là số nguyên dương.')).toBeInTheDocument();
  });

  it('fetches effective permissions from existing endpoint', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(mockedApi.fetchEffectivePermissions).toHaveBeenCalledWith(42, null);
    });
  });

  it('displays backend-authoritative final permission codes', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('effective-permissions-card')).toBeInTheDocument();
      expect(screen.getByText('Quyền hiệu lực do Backend xác định')).toBeInTheDocument();
      expect(screen.getByText('SECURITY_ADMIN_MANAGE')).toBeInTheDocument();
      expect(screen.getByText('ORGANIZATION_USER_MANAGE')).toBeInTheDocument();
    });
  });

  it('enriches codes from permission catalog', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByText('Manage security administration')).toBeInTheDocument();
      expect(screen.getByText('Manage organization users')).toBeInTheDocument();
    });
  });

  it('handles permission codes missing from catalog safely', async () => {
    mockedApi.fetchEffectivePermissions.mockResolvedValue({
      userId: 42,
      companyId: null,
      permissionCodes: ['UNKNOWN_PERM'],
    });
    mockedApi.fetchPermissionCatalog.mockResolvedValue([]);

    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByText('UNKNOWN_PERM')).toBeInTheDocument();
    });
  });

  it('renders empty state safely', async () => {
    mockedApi.fetchEffectivePermissions.mockResolvedValue({
      userId: 42,
      companyId: null,
      permissionCodes: [],
    });

    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('no-permissions-message')).toBeInTheDocument();
    });
  });

  it('renders loading state safely', async () => {
    mockedApi.fetchEffectivePermissions.mockReturnValue(new Promise(() => {}));

    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('effective-loading')).toBeInTheDocument();
    });
  });

  it('renders sanitized failure state', async () => {
    mockedApi.fetchEffectivePermissions.mockRejectedValue({
      response: { status: 404 },
    });

    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '999' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('effective-error')).toBeInTheDocument();
      expect(screen.getByText('User not found.')).toBeInTheDocument();
    });
  });

  it('renders 403 error as permission denied message', async () => {
    mockedApi.fetchEffectivePermissions.mockRejectedValue({
      response: { status: 403 },
    });

    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByText('You do not have permission to view effective permissions.')).toBeInTheDocument();
    });
  });

  it('passes company context to effective permissions API', async () => {
    mockCompanyId = 100;
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(mockedApi.fetchEffectivePermissions).toHaveBeenCalledWith(42, 100);
    });
  });

  it('shows global context indicator when no company is selected', async () => {
    mockCompanyId = null;
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('global-context-indicator')).toBeInTheDocument();
    });
  });

  it('shows company context indicator when company is selected', () => {
    mockCompanyId = 100;
    renderComponent();
    expect(screen.getByTestId('company-context-indicator')).toBeInTheDocument();
    // Neo đầu/cuối: regex không neo sẽ đậu cả với "…: 1000" hay "…: 100 (sai)".
    expect(screen.getByText(/^Bối cảnh công ty:\s*100$/)).toBeInTheDocument();
  });

  it('does not show source attribution as authoritative', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('contextual-sections-card')).toBeInTheDocument();
      expect(screen.getByTestId('context-disclaimer')).toBeInTheDocument();
      expect(screen.getByTestId('page-description')).toHaveTextContent('Không có thông tin phân bổ theo nguồn.');
    });
  });

  it('does not show mutation controls', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('effective-permissions-card')).toBeInTheDocument();
    });

    expect(screen.queryByText('Add')).not.toBeInTheDocument();
    expect(screen.queryByText('Remove')).not.toBeInTheDocument();
    expect(screen.queryByText('Save')).not.toBeInTheDocument();
    expect(screen.queryByText('Delete')).not.toBeInTheDocument();
  });

  it('does not show bulk controls', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('effective-permissions-card')).toBeInTheDocument();
    });

    expect(screen.queryByText('Bulk')).not.toBeInTheDocument();
  });

  it('does not show export or download controls', async () => {
    renderComponent();
    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '42' } });
    fireEvent.click(screen.getByTestId('lookup-button'));

    await waitFor(() => {
      expect(screen.getByTestId('effective-permissions-card')).toBeInTheDocument();
    });

    expect(screen.queryByText('Export')).not.toBeInTheDocument();
    expect(screen.queryByText('Download')).not.toBeInTheDocument();
  });

  it('core workflow does not require SECURITY_ACCOUNT_MANAGE', () => {
    renderComponent();
    expect(screen.getByTestId('user-id-input')).toBeInTheDocument();
    expect(screen.getByTestId('lookup-button')).toBeInTheDocument();
  });

  it('clears validation error on input change', () => {
    renderComponent();
    fireEvent.click(screen.getByTestId('lookup-button'));
    expect(screen.getByTestId('validation-error')).toBeInTheDocument();

    fireEvent.change(screen.getByTestId('user-id-input'), { target: { value: '1' } });
    expect(screen.queryByTestId('validation-error')).not.toBeInTheDocument();
  });

  it('submits on enter key press', async () => {
    renderComponent();
    const input = screen.getByTestId('user-id-input');
    fireEvent.change(input, { target: { value: '42' } });
    fireEvent.keyDown(input, { key: 'Enter', code: 'Enter' });

    await waitFor(() => {
      expect(mockedApi.fetchEffectivePermissions).toHaveBeenCalledWith(42, null);
    });
  });
});
