import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import AuthenticatedShell from './AuthenticatedShell';

const mockUseAuth = vi.fn();
vi.mock('../auth/AuthProvider', () => ({
  useAuth: () => mockUseAuth(),
  usePermissions: () => ({
    hasPermission: mockHasPermission,
  }),
}));

const mockUseCompany = vi.fn();
vi.mock('../auth/CompanyProvider', () => ({
  useCompany: () => mockUseCompany(),
}));

// Chuông thông báo gọi getMyApprovals — chặn mọi HTTP thật trong test.
const mockGetMyApprovals = vi.fn();
vi.mock('../workflow/workflowRuntimeApi', () => ({
  getMyApprovals: () => mockGetMyApprovals(),
}));

let mockHasPermission = vi.fn();

// Menu antd dạng inline chỉ dựng DOM cho nhóm con đang mở (defaultOpenKeys theo
// đường dẫn hiện tại), nên mỗi test phải render tại route của nhóm cần kiểm tra.
const renderShell = (initialPath = '/') => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[initialPath]}>
        <AuthenticatedShell />
      </MemoryRouter>
    </QueryClientProvider>
  );
};

describe('AuthenticatedShell Navigation Gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { username: 'testuser', displayName: 'Test User' },
      logout: vi.fn(),
    });
    mockUseCompany.mockReturnValue({
      companies: [],
      currentCompanyId: null,
      switchCompany: vi.fn(),
    });
    mockHasPermission = vi.fn().mockReturnValue(false);
    mockGetMyApprovals.mockResolvedValue([]);
  });

  it('hides SECURITY_ADMIN_MANAGE-gated items when permission is missing', () => {
    mockHasPermission.mockImplementation((perm: string) => perm !== 'SECURITY_ADMIN_MANAGE');
    renderShell('/security/accounts');
    expect(screen.queryByTestId('nav-security-roles')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-admin-groups')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-departments-permissions')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-effective-permissions')).not.toBeInTheDocument();
  });

  it('shows SECURITY_ADMIN_MANAGE-gated items when permission is present', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_ADMIN_MANAGE');
    renderShell('/security');
    expect(screen.getByTestId('nav-security-roles')).toBeInTheDocument();
    expect(screen.getByTestId('nav-security-admin-groups')).toBeInTheDocument();
    expect(screen.getByTestId('nav-security-departments-permissions')).toBeInTheDocument();
    expect(screen.getByTestId('nav-security-effective-permissions')).toBeInTheDocument();
  });

  it('does not show SECURITY_ADMIN_MANAGE-gated items for SECURITY_AUDIT_VIEW alone', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_AUDIT_VIEW');
    renderShell('/security');
    expect(screen.queryByTestId('nav-security-roles')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-admin-groups')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-departments-permissions')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-effective-permissions')).not.toBeInTheDocument();
    expect(screen.getByTestId('nav-security-audit')).toBeInTheDocument();
  });

  it('does not show SECURITY_ADMIN_MANAGE-gated items for SECURITY_ACCOUNT_MANAGE alone', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_ACCOUNT_MANAGE');
    renderShell('/security');
    expect(screen.queryByTestId('nav-security-roles')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-admin-groups')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-departments-permissions')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-security-effective-permissions')).not.toBeInTheDocument();
    expect(screen.getByTestId('nav-security-accounts')).toBeInTheDocument();
  });

  it('shows Customers menu item when CUSTOMER_VIEW_BASIC is granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'CUSTOMER_VIEW_BASIC');
    renderShell('/customers');
    expect(screen.getByTestId('nav-customers-group')).toBeInTheDocument();
    expect(screen.getByTestId('nav-customers')).toBeInTheDocument();
  });

  it('hides Customers menu item when CUSTOMER_VIEW_BASIC is not granted', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/customers');
    expect(screen.queryByTestId('nav-customers-group')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-customers')).not.toBeInTheDocument();
  });

  it('shows My Proposals menu item when CUSTOMER_CHANGE_REQUEST_CREATE is granted', () => {
    mockHasPermission.mockImplementation(
      (perm: string) => perm === 'CUSTOMER_CHANGE_REQUEST_CREATE' || perm === 'CUSTOMER_VIEW_BASIC'
    );
    renderShell('/customers');
    expect(screen.getByTestId('nav-customers-proposals')).toBeInTheDocument();
  });

  it('hides My Proposals menu item when CUSTOMER_CHANGE_REQUEST_CREATE is not granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'CUSTOMER_VIEW_BASIC');
    renderShell('/customers');
    expect(screen.getByTestId('nav-customers')).toBeInTheDocument();
    expect(screen.queryByTestId('nav-customers-proposals')).not.toBeInTheDocument();
  });

  it('shows Workflow Admin menu item when WORKFLOW_VIEW is granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'WORKFLOW_VIEW');
    renderShell('/workflow');
    expect(screen.getByTestId('nav-workflow')).toBeInTheDocument();
    expect(screen.getByTestId('nav-workflow-bindings')).toBeInTheDocument();
    expect(screen.getByTestId('nav-workflow-instances')).toBeInTheDocument();
  });

  it('hides Workflow Admin menu item when WORKFLOW_VIEW is not granted', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.queryByTestId('nav-workflow')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-workflow-bindings')).not.toBeInTheDocument();
    expect(screen.queryByTestId('nav-workflow-instances')).not.toBeInTheDocument();
  });

  it('shows My Approvals menu item to all authenticated users without permission gate', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.getByTestId('nav-workflow-my-approvals')).toBeInTheDocument();
  });

  it('shows My Requests menu item to all authenticated users without permission gate', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.getByTestId('nav-workflow-my-requests')).toBeInTheDocument();
  });

  it('shows APPROVAL_AUTHORITY_MANAGE-gated item only when permission is granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'APPROVAL_AUTHORITY_MANAGE');
    renderShell('/workflow');
    expect(screen.getByTestId('nav-workflow-authorities')).toBeInTheDocument();
  });

  it('hides APPROVAL_AUTHORITY_MANAGE-gated item when permission is missing', () => {
    // Nửa phủ định: thiếu nó thì test trên chỉ chứng minh "hiện được", không chứng minh "có chặn".
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.queryByTestId('nav-workflow-authorities')).not.toBeInTheDocument();
  });
});

describe('AuthenticatedShell sidebar accordion', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { username: 'testuser', displayName: 'Test User' },
      logout: vi.fn(),
    });
    mockUseCompany.mockReturnValue({
      companies: [],
      currentCompanyId: null,
      switchCompany: vi.fn(),
    });
    mockHasPermission = vi.fn().mockReturnValue(true);
    mockGetMyApprovals.mockResolvedValue([]);
  });

  // Nhóm cha đã từng mở thì antd giữ lại DOM con (chỉ ẩn đi), nên không kiểm bằng
  // "con còn trong DOM không" mà kiểm cờ mở/đóng ngay trên thẻ nhóm cha.
  const submenuOf = (testId: string) =>
    screen.getByTestId(testId).closest('.ant-menu-submenu') as HTMLElement;

  it('collapses the previously open group when another parent group is opened', async () => {
    const user = userEvent.setup();
    renderShell('/');

    await user.click(screen.getByTestId('nav-customers-group'));
    await waitFor(() => expect(submenuOf('nav-customers-group')).toHaveClass('ant-menu-submenu-open'));

    await user.click(screen.getByTestId('nav-workflow-group'));
    await waitFor(() => expect(submenuOf('nav-workflow-group')).toHaveClass('ant-menu-submenu-open'));
    expect(submenuOf('nav-customers-group')).not.toHaveClass('ant-menu-submenu-open');
  });

  it('closes the group when its own header is clicked again', async () => {
    const user = userEvent.setup();
    renderShell('/');

    await user.click(screen.getByTestId('nav-security-group'));
    await waitFor(() => expect(submenuOf('nav-security-group')).toHaveClass('ant-menu-submenu-open'));

    await user.click(screen.getByTestId('nav-security-group'));
    await waitFor(() => expect(submenuOf('nav-security-group')).not.toHaveClass('ant-menu-submenu-open'));
  });

  it('restores the open group after the sider is collapsed and expanded', async () => {
    const user = userEvent.setup();
    renderShell('/security/roles');
    expect(submenuOf('nav-security-group')).toHaveClass('ant-menu-submenu-open');

    await user.click(screen.getByTestId('sider-toggle'));
    await user.click(screen.getByTestId('sider-toggle'));

    await waitFor(() => expect(submenuOf('nav-security-group')).toHaveClass('ant-menu-submenu-open'));
  });

  it('opens only the group that contains the current route', () => {
    renderShell('/security/roles');
    expect(submenuOf('nav-security-group')).toHaveClass('ant-menu-submenu-open');
    expect(submenuOf('nav-customers-group')).not.toHaveClass('ant-menu-submenu-open');
    expect(submenuOf('nav-workflow-group')).not.toHaveClass('ant-menu-submenu-open');
  });
});

describe('AuthenticatedShell header', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { username: 'testuser', displayName: 'Test User' },
      logout: vi.fn(),
    });
    mockUseCompany.mockReturnValue({
      companies: [],
      currentCompanyId: null,
      switchCompany: vi.fn(),
    });
    mockHasPermission = vi.fn().mockReturnValue(false);
    mockGetMyApprovals.mockResolvedValue([]);
  });

  it('renders the notification bell', () => {
    renderShell('/');
    expect(screen.getByTestId('notification-bell')).toBeInTheDocument();
  });

  it('shows the pending approval count on the bell badge', async () => {
    mockGetMyApprovals.mockResolvedValue([{ instanceId: 1 }, { instanceId: 2 }]);
    renderShell('/');
    expect(await screen.findByTitle('2')).toBeInTheDocument();
  });
});
