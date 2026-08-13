import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
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
  getAccountsByUserId: vi.fn(),
}));

vi.mock('../auth/CompanyProvider', () => ({
  useCompany: vi.fn(),
}));

const MOCK_CATALOG = [
  {
    permissionCode: 'SECURITY_ADMIN_MANAGE',
    moduleCode: 'SECURITY',
    actionCode: 'ADMIN_MANAGE',
    dataScope: 'GLOBAL',
    isSensitive: true,
    isDelegable: false,
    requiresReason: true,
    isActive: true,
    description: 'Quản trị bảo mật',
  },
];

const MOCK_ASSIGNMENTS = [
  {
    id: 1,
    userId: 100,
    permissionCode: 'SECURITY_ADMIN_MANAGE',
    scopeType: 'GLOBAL',
    companyId: null,
    grantType: 'ALLOW',
    assignmentStatus: 'ACTIVE',
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: null,
    reason: 'test',
    rowVersion: 'v1',
  },
];

const MOCK_EFFECTIVE = {
  userId: 100,
  companyId: null,
  permissionCodes: ['SECURITY_ADMIN_MANAGE'],
};

const MOCK_ACCOUNTS = {
  page: 1,
  pageSize: 20,
  totalCount: 1,
  items: [
    {
      accountId: 42,
      userId: 100,
      username: 'alice',
      fullName: 'Alice Nguyen',
      employeeCode: 'EMP1',
      providerType: 'INTERNAL',
      status: 'ACTIVE',
      mustChangePassword: false,
      employmentStatus: 'ACTIVE',
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
    },
  ],
};

const USER_OPTION_LABEL = 'Alice Nguyen — alice · EMP1';

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

/** Chọn người dùng "alice" qua ô chọn ở bước 1. */
async function selectAliceUser(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByTestId('user-search-input'));
  const option = await screen.findByText(USER_OPTION_LABEL);
  await user.click(option);
}

describe('PermissionAssignmentPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (useCompany as ReturnType<typeof vi.fn>).mockReturnValue({
      currentCompanyId: null,
      companies: [],
    });
    (accountApi.searchAccounts as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACCOUNTS);
    (accountApi.getAccountsByUserId as ReturnType<typeof vi.fn>).mockResolvedValue([]);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('hiển thị trang và tải danh mục quyền', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);

    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    expect(screen.getByTestId('permission-assignment-loading')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId('permission-assignment-page')).toBeInTheDocument();
    });
    expect(screen.getByText('Phân quyền cá nhân')).toBeInTheDocument();
  });

  it('chọn người dùng và hiển thị họ tên thay vì mã số', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ASSIGNMENTS);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_EFFECTIVE);

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-selection-card')).toBeInTheDocument());
    await selectAliceUser(user);

    // Banner người dùng hiển thị họ tên + tên đăng nhập + mã NV (không phải "Người dùng: 100")
    const banner = await screen.findByTestId('selected-user-info');
    expect(within(banner).getByText('Alice Nguyen')).toBeInTheDocument();
    expect(within(banner).getByText('alice')).toBeInTheDocument();
    expect(within(banner).getByText('EMP1')).toBeInTheDocument();
    expect(screen.queryByText('Người dùng đã chọn: 100')).not.toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId('assignments-card')).toBeInTheDocument();
      expect(screen.getByTestId('effective-permissions-card')).toBeInTheDocument();
    });
  });

  it('hiển thị quyền cá nhân với nhãn tiếng Việt', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ASSIGNMENTS);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_EFFECTIVE);

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-selection-card')).toBeInTheDocument());
    await selectAliceUser(user);

    const list = await screen.findByTestId('assignments-list');
    // Tên quyền dễ đọc + mã quyền phụ
    expect(within(list).getByText('Quản trị bảo mật')).toBeInTheDocument();
    expect(within(list).getByText('SECURITY_ADMIN_MANAGE')).toBeInTheDocument();
    // Nhãn đã Việt hóa
    expect(within(list).getByText('Cho phép')).toBeInTheDocument();
    expect(within(list).getByText('Toàn hệ thống')).toBeInTheDocument();
    // Không còn mã tiếng Anh thô trong bảng
    expect(within(list).queryByText('ALLOW')).not.toBeInTheDocument();
    expect(within(list).queryByText('GLOBAL')).not.toBeInTheDocument();
  });

  it('mở modal cấp quyền và báo lỗi khi chưa chọn quyền', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue({ userId: 100, companyId: null, permissionCodes: [] });

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-selection-card')).toBeInTheDocument());
    await selectAliceUser(user);

    await waitFor(() => expect(screen.getByTestId('grant-permission-button')).toBeInTheDocument());
    await user.click(screen.getByTestId('grant-permission-button'));

    expect(screen.getByTestId('grant-permission-modal')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Cấp quyền' }));

    expect(screen.getByTestId('grant-validation-error')).toHaveTextContent('Vui lòng chọn một quyền.');
  });

  it('bắt buộc lý do với quyền requiresReason', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue({ userId: 100, companyId: null, permissionCodes: [] });

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-selection-card')).toBeInTheDocument());
    await selectAliceUser(user);

    await waitFor(() => expect(screen.getByTestId('grant-permission-button')).toBeInTheDocument());
    await user.click(screen.getByTestId('grant-permission-button'));

    await user.click(screen.getByRole('combobox', { name: 'Chọn quyền' }));
    await user.click(await screen.findByText('Quản trị bảo mật (SECURITY_ADMIN_MANAGE)'));

    await user.click(screen.getByRole('button', { name: 'Cấp quyền' }));

    expect(screen.getByTestId('grant-validation-error')).toHaveTextContent('Quyền này yêu cầu nhập lý do.');
    expect(api.grantIndividualPermission).not.toHaveBeenCalled();
  });

  it('gửi yêu cầu cấp quyền thành công', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue({ userId: 100, companyId: null, permissionCodes: [] });
    (api.grantIndividualPermission as ReturnType<typeof vi.fn>).mockResolvedValue({ ...MOCK_ASSIGNMENTS[0] });

    const user = userEvent.setup();
    render(<PermissionAssignmentPage />, { wrapper: makeWrapper() });

    await waitFor(() => expect(screen.getByTestId('user-selection-card')).toBeInTheDocument());
    await selectAliceUser(user);

    await waitFor(() => expect(screen.getByTestId('grant-permission-button')).toBeInTheDocument());
    await user.click(screen.getByTestId('grant-permission-button'));

    await user.click(screen.getByRole('combobox', { name: 'Chọn quyền' }));
    await user.click(await screen.findByText('Quản trị bảo mật (SECURITY_ADMIN_MANAGE)'));

    // Quyền này bắt buộc lý do → nhập lý do
    await user.type(screen.getByPlaceholderText('Nhập lý do cấp quyền'), 'Cấp cho quản trị viên');

    await user.click(screen.getByRole('button', { name: 'Cấp quyền' }));

    await waitFor(() => {
      expect(api.grantIndividualPermission).toHaveBeenCalledWith(100, expect.objectContaining({
        permissionCode: 'SECURITY_ADMIN_MANAGE',
        scopeType: 'GLOBAL',
        grantType: 'ALLOW',
        reason: 'Cấp cho quản trị viên',
      }));
    });

    await waitFor(() => expect(screen.getByTestId('success-message')).toBeInTheDocument());
  });

  it('hydrate họ tên khi mở bằng liên kết ?userId=', async () => {
    (api.fetchPermissionCatalog as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_CATALOG);
    (api.fetchUserIndividualPermissions as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (api.fetchEffectivePermissions as ReturnType<typeof vi.fn>).mockResolvedValue({ userId: 100, companyId: null, permissionCodes: [] });
    (accountApi.getAccountsByUserId as ReturnType<typeof vi.fn>).mockResolvedValue(MOCK_ACCOUNTS.items);

    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={qc}>
        <MemoryRouter initialEntries={['/security/permissions/assignments?userId=100']}>
          <PermissionAssignmentPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    const banner = await screen.findByTestId('selected-user-info');
    expect(within(banner).getByText('Alice Nguyen')).toBeInTheDocument();
    expect(accountApi.getAccountsByUserId).toHaveBeenCalledWith(100);
  });
});
