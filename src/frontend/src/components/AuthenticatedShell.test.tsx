import { render, screen } from '@testing-library/react';
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
    expect(screen.queryByText('Vai trò')).not.toBeInTheDocument();
    expect(screen.queryByText('Nhóm quản trị')).not.toBeInTheDocument();
    expect(screen.queryByText('Quyền phòng ban')).not.toBeInTheDocument();
    expect(screen.queryByText('Kiểm tra quyền')).not.toBeInTheDocument();
  });

  it('shows SECURITY_ADMIN_MANAGE-gated items when permission is present', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_ADMIN_MANAGE');
    renderShell('/security');
    expect(screen.getByText('Vai trò')).toBeInTheDocument();
    expect(screen.getByText('Nhóm quản trị')).toBeInTheDocument();
    expect(screen.getByText('Quyền phòng ban')).toBeInTheDocument();
    expect(screen.getByText('Kiểm tra quyền')).toBeInTheDocument();
  });

  it('does not show SECURITY_ADMIN_MANAGE-gated items for SECURITY_AUDIT_VIEW alone', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_AUDIT_VIEW');
    renderShell('/security');
    expect(screen.queryByText('Vai trò')).not.toBeInTheDocument();
    expect(screen.queryByText('Nhóm quản trị')).not.toBeInTheDocument();
    expect(screen.queryByText('Quyền phòng ban')).not.toBeInTheDocument();
    expect(screen.queryByText('Kiểm tra quyền')).not.toBeInTheDocument();
    expect(screen.getByText('Nhật ký kiểm toán')).toBeInTheDocument();
  });

  it('does not show SECURITY_ADMIN_MANAGE-gated items for SECURITY_ACCOUNT_MANAGE alone', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'SECURITY_ACCOUNT_MANAGE');
    renderShell('/security');
    expect(screen.queryByText('Vai trò')).not.toBeInTheDocument();
    expect(screen.queryByText('Nhóm quản trị')).not.toBeInTheDocument();
    expect(screen.queryByText('Quyền phòng ban')).not.toBeInTheDocument();
    expect(screen.queryByText('Kiểm tra quyền')).not.toBeInTheDocument();
    expect(screen.getByText('Tài khoản')).toBeInTheDocument();
  });

  it('shows Customers menu item when CUSTOMER_VIEW_BASIC is granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'CUSTOMER_VIEW_BASIC');
    renderShell('/customers');
    expect(screen.getByText('Khách hàng')).toBeInTheDocument();
    expect(screen.getByText('Danh sách KH')).toBeInTheDocument();
  });

  it('hides Customers menu item when CUSTOMER_VIEW_BASIC is not granted', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/customers');
    expect(screen.queryByText('Khách hàng')).not.toBeInTheDocument();
    expect(screen.queryByText('Danh sách KH')).not.toBeInTheDocument();
  });

  it('shows My Proposals menu item when CUSTOMER_CHANGE_REQUEST_CREATE is granted', () => {
    mockHasPermission.mockImplementation(
      (perm: string) => perm === 'CUSTOMER_CHANGE_REQUEST_CREATE' || perm === 'CUSTOMER_VIEW_BASIC'
    );
    renderShell('/customers');
    expect(screen.getByText('Đề xuất KH mới')).toBeInTheDocument();
  });

  it('hides My Proposals menu item when CUSTOMER_CHANGE_REQUEST_CREATE is not granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'CUSTOMER_VIEW_BASIC');
    renderShell('/customers');
    expect(screen.getByText('Danh sách KH')).toBeInTheDocument();
    expect(screen.queryByText('Đề xuất KH mới')).not.toBeInTheDocument();
  });

  it('shows Workflow Admin menu item when WORKFLOW_VIEW is granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'WORKFLOW_VIEW');
    renderShell('/workflow');
    expect(screen.getByText('Quản trị quy trình')).toBeInTheDocument();
    expect(screen.getByText('Liên kết quy trình')).toBeInTheDocument();
    expect(screen.getByText('Tất cả hồ sơ')).toBeInTheDocument();
  });

  it('hides Workflow Admin menu item when WORKFLOW_VIEW is not granted', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.queryByText('Quản trị quy trình')).not.toBeInTheDocument();
    expect(screen.queryByText('Liên kết quy trình')).not.toBeInTheDocument();
    expect(screen.queryByText('Tất cả hồ sơ')).not.toBeInTheDocument();
  });

  it('shows My Approvals menu item to all authenticated users without permission gate', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.getByText('Chờ duyệt')).toBeInTheDocument();
  });

  it('shows My Requests menu item to all authenticated users without permission gate', () => {
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.getByText('Yêu cầu của tôi')).toBeInTheDocument();
  });

  it('shows APPROVAL_AUTHORITY_MANAGE-gated item only when permission is granted', () => {
    mockHasPermission.mockImplementation((perm: string) => perm === 'APPROVAL_AUTHORITY_MANAGE');
    renderShell('/workflow');
    expect(screen.getByText('Thẩm quyền phê duyệt')).toBeInTheDocument();
  });

  it('hides APPROVAL_AUTHORITY_MANAGE-gated item when permission is missing', () => {
    // Nửa phủ định: thiếu nó thì test trên chỉ chứng minh "hiện được", không chứng minh "có chặn".
    mockHasPermission.mockReturnValue(false);
    renderShell('/workflow');
    expect(screen.queryByText('Thẩm quyền phê duyệt')).not.toBeInTheDocument();
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
