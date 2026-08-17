import React, { useEffect, useRef, useState } from 'react';
import { Badge, Button, Layout, Menu, Typography, Select, Avatar, Dropdown, theme } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getMyApprovals } from '../workflow/workflowRuntimeApi';
import {
  HomeOutlined,
  TeamOutlined,
  SafetyOutlined,
  SafetyCertificateOutlined,
  DollarOutlined,
  ToolOutlined,
  ApartmentOutlined,
  UserOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  HeartOutlined,
  CreditCardOutlined,
  AuditOutlined,
  FileSearchOutlined,
  CheckSquareOutlined,
  SwapOutlined,
  IdcardOutlined,
  SettingOutlined,
  KeyOutlined,
  LockOutlined,
  BankOutlined,
  ReconciliationOutlined,
  EnvironmentOutlined,
  TagsOutlined,
  BellOutlined,
} from '@ant-design/icons';
import { useCompany } from '../auth/CompanyProvider';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuth, usePermissions } from '../auth/AuthProvider';
import type { MenuProps } from 'antd';

const { Header, Content, Sider } = Layout;

/**
 * Bọc nhãn menu bằng span có định danh cố định.
 *
 * Vì sao: antd Menu dùng API `items` nên không nhận data-testid trực tiếp trên từng mục.
 * Không có định danh thì test kiểm phân quyền buộc phải khớp CHỮ HIỂN THỊ — đổi một chữ
 * tiếng Việt là hàng loạt test vỡ, đúng khoản nợ vừa phải dọn.
 */
const navLabel = (testId: string, text: string) => <span data-testid={testId}>{text}</span>;

/**
 * Nhóm menu cha ứng với từng khu vực đường dẫn.
 *
 * Vì sao tách ra hằng số: dùng cho cả 3 việc — mở sẵn nhóm chứa trang đang xem,
 * mở lại nhóm khi điều hướng sang khu vực khác, và nhận biết đâu là nhóm cấp gốc
 * để thu các nhóm còn lại (kiểu accordion).
 */
const MENU_GROUPS: ReadonlyArray<{ key: string; prefixes: string[] }> = [
  { key: 'customers-group', prefixes: ['/customers'] },
  { key: 'payments-group', prefixes: ['/payments', '/reconciliation'] },
  { key: 'services-group', prefixes: ['/services', '/care-packages'] },
  { key: 'workflow-group', prefixes: ['/workflow'] },
  { key: 'security-group', prefixes: ['/security'] },
  { key: 'org-group', prefixes: ['/organizations'] },
  { key: 'graves-group', prefixes: ['/graves'] },
  { key: 'cards-group', prefixes: ['/cards'] },
];

const ROOT_GROUP_KEYS = MENU_GROUPS.map(g => g.key);

const groupKeyForPath = (path: string): string | null =>
  MENU_GROUPS.find(g => g.prefixes.some(p => path.startsWith(p)))?.key ?? null;

const AuthenticatedShell: React.FC = () => {
  const { logout, user } = useAuth();
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const location = useLocation();
  const { companies, currentCompanyId, switchCompany } = useCompany();
  const [collapsed, setCollapsed] = useState(false);
  const { token } = theme.useToken();

  // Nhóm menu đang mở. Điều khiển bằng state (không phải defaultOpenKeys) để ép
  // luật accordion: cùng lúc chỉ một nhóm cha xoè ra.
  const currentGroupKey = groupKeyForPath(location.pathname);
  const [openKeys, setOpenKeys] = useState<string[]>(currentGroupKey ? [currentGroupKey] : []);

  // Điều hướng sang khu vực khác (bấm chuông, nút trong trang, gõ URL...) thì mở
  // nhóm chứa trang đó. Nếu nhóm đã mở sẵn thì giữ nguyên — tránh việc người dùng
  // vừa tự thu nhóm lại bị bung ra.
  useEffect(() => {
    if (!currentGroupKey) return;
    setOpenKeys(prev => (prev.includes(currentGroupKey) ? prev : [currentGroupKey]));
  }, [currentGroupKey]);

  // Thu gọn thanh bên: antd đổi menu sang dạng popup và tự xoá sạch openKeys (báo về
  // qua onOpenChange). Cất lại nhóm đang mở để bung thanh bên ra thì trả về đúng chỗ cũ —
  // không có đoạn này, thu vào mở ra là mọi nhóm đóng hết.
  const openKeysBeforeCollapse = useRef<string[]>(openKeys);
  useEffect(() => {
    if (collapsed) {
      // Hiệu ứng của Menu (con) chạy trước hiệu ứng này, nên `openKeys` ở đây vẫn là
      // giá trị trước khi bị xoá.
      openKeysBeforeCollapse.current = openKeys;
    } else {
      setOpenKeys(openKeysBeforeCollapse.current);
    }
    // Chỉ chụp/khôi phục đúng lúc đóng-mở thanh bên, nên không đưa openKeys vào deps.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [collapsed]);

  /**
   * Mở một nhóm cha thì thu mọi nhóm cha khác; thao tác đóng thì giữ nguyên phần còn lại.
   */
  const handleOpenChange: MenuProps['onOpenChange'] = keys => {
    const justOpened = keys.find(key => !openKeys.includes(key as string)) as string | undefined;
    if (justOpened && ROOT_GROUP_KEYS.includes(justOpened)) {
      setOpenKeys([justOpened]);
      return;
    }
    setOpenKeys(keys as string[]);
  };

  // Đếm việc chờ duyệt cho chuông thông báo. Tự làm mới định kỳ + khi quay lại cửa sổ.
  const { data: pendingApprovals } = useQuery({
    queryKey: ['header-pending-approvals'],
    queryFn: getMyApprovals,
    refetchInterval: 60000,
    refetchOnWindowFocus: true,
    staleTime: 30000,
  });
  const pendingCount = pendingApprovals?.length ?? 0;

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  const menuItems: MenuProps['items'] = [
    {
      key: '/',
      icon: <HomeOutlined />,
      label: navLabel('nav-home', 'Trang chủ'),
      onClick: () => navigate('/'),
    },
    hasPermission('CUSTOMER_VIEW_BASIC') ? {
      key: 'customers-group',
      icon: <TeamOutlined />,
      label: navLabel('nav-customers-group', 'Khách hàng'),
      children: [
        {
          key: '/customers',
          label: navLabel('nav-customers', 'Danh sách KH'),
          icon: <TeamOutlined />,
          onClick: () => navigate('/customers'),
        },
        hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE') ? {
          key: '/customers/proposals',
          label: navLabel('nav-customers-proposals', 'Đề xuất KH mới'),
          icon: <FileSearchOutlined />,
          onClick: () => navigate('/customers/proposals'),
        } : null,
        hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE') ? {
          key: '/customers/change-requests',
          label: navLabel('nav-customers-change-requests', 'Yêu cầu thay đổi'),
          icon: <SwapOutlined />,
          onClick: () => navigate('/customers/change-requests'),
        } : null,
        hasPermission('CUSTOMER_MERGE_REQUEST_VIEW') ? {
          key: '/customers/merge-requests',
          label: navLabel('nav-customers-merge-requests', 'Gộp khách hàng'),
          icon: <SwapOutlined />,
          onClick: () => navigate('/customers/merge-requests'),
        } : null,
        hasPermission('CUSTOMER_MERGE_REQUEST_CREATE') ? {
          key: '/customers/merge/search',
          label: navLabel('nav-customers-merge-search', 'Tìm trùng lặp'),
          icon: <FileSearchOutlined />,
          onClick: () => navigate('/customers/merge/search'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('GRAVE_VIEW') ? {
      key: 'graves-group',
      icon: <EnvironmentOutlined />,
      label: navLabel('nav-graves-group', 'Quản lý mộ'),
      children: [
        {
          key: '/graves',
          label: navLabel('nav-graves', 'Danh sách mộ'),
          icon: <EnvironmentOutlined />,
          onClick: () => navigate('/graves'),
        },
        {
          key: '/graves/attachments-summary',
          label: navLabel('nav-graves-attachments', 'Tổng hợp giấy tờ'),
          icon: <EnvironmentOutlined />,
          onClick: () => navigate('/graves/attachments-summary'),
        },
      ],
    } : null,
    hasPermission('PAYMENT_CREATE_DRAFT') || hasPermission('RECONCILIATION_PREPARE') ? {
      key: 'payments-group',
      icon: <DollarOutlined />,
      label: navLabel('nav-payments-group', 'Thanh toán'),
      children: [
        hasPermission('PAYMENT_CREATE_DRAFT') ? {
          key: '/payments',
          label: navLabel('nav-payments', 'Giao dịch'),
          icon: <DollarOutlined />,
          onClick: () => navigate('/payments'),
        } : null,
        hasPermission('RECONCILIATION_PREPARE') ? {
          key: '/reconciliation/daily',
          label: navLabel('nav-reconciliation-daily', 'Đối soát ngày'),
          icon: <ReconciliationOutlined />,
          onClick: () => navigate('/reconciliation/daily'),
        } : null,
        hasPermission('RECONCILIATION_PREPARE') ? {
          key: '/reconciliation/monthly',
          label: navLabel('nav-reconciliation-monthly', 'Đối soát tháng'),
          icon: <ReconciliationOutlined />,
          onClick: () => navigate('/reconciliation/monthly'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('SERVICE_TYPE_MANAGE') || hasPermission('SERVICE_VIEW') ? {
      key: 'services-group',
      icon: <ToolOutlined />,
      label: navLabel('nav-services-group', 'Dịch vụ'),
      children: [
        hasPermission('SERVICE_TYPE_MANAGE') ? {
          key: '/services/types',
          label: navLabel('nav-services-types', 'Gói dịch vụ'),
          icon: <SettingOutlined />,
          onClick: () => navigate('/services/types'),
        } : null,
        hasPermission('SERVICE_VIEW') ? {
          key: '/services',
          label: navLabel('nav-services', 'Bảng tổng hợp dịch vụ'),
          icon: <ToolOutlined />,
          onClick: () => navigate('/services'),
        } : null,
        {
          key: '/care-packages',
          label: navLabel('nav-care-packages', 'Bán gói chăm sóc'),
          icon: <HeartOutlined />,
          onClick: () => navigate('/care-packages'),
        },
      ].filter(Boolean),
    } : null,
    {
      key: 'cards-group',
      icon: <CreditCardOutlined />,
      label: navLabel('nav-cards-group', 'Thẻ mộ'),
      children: [
        {
          key: '/cards',
          label: navLabel('nav-cards', 'Danh sách thẻ'),
          onClick: () => navigate('/cards'),
        },
        {
          key: '/cards/reprints',
          label: navLabel('nav-cards-reprints', 'Yêu cầu in lại'),
          onClick: () => navigate('/cards/reprints'),
        },
        {
          key: '/cards/watermarks',
          label: navLabel('nav-cards-watermarks', 'Hoa văn thẻ'),
          onClick: () => navigate('/cards/watermarks'),
        },
      ],
    },
    {
      key: 'workflow-group',
      icon: <ApartmentOutlined />,
      label: navLabel('nav-workflow-group', 'Quy trình'),
      children: [
        {
          key: '/workflow/my-approvals',
          label: navLabel('nav-workflow-my-approvals', 'Chờ duyệt'),
          icon: <CheckSquareOutlined />,
          onClick: () => navigate('/workflow/my-approvals'),
        },
        {
          key: '/workflow/my-requests',
          label: navLabel('nav-workflow-my-requests', 'Yêu cầu của tôi'),
          icon: <AuditOutlined />,
          onClick: () => navigate('/workflow/my-requests'),
        },
        hasPermission('WORKFLOW_VIEW') ? {
          key: '/workflow',
          label: navLabel('nav-workflow', 'Quản trị quy trình'),
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/workflow'),
        } : null,
        hasPermission('WORKFLOW_VIEW') ? {
          key: '/workflow/bindings',
          label: navLabel('nav-workflow-bindings', 'Liên kết quy trình'),
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/workflow/bindings'),
        } : null,
        hasPermission('WORKFLOW_VIEW') ? {
          key: '/workflow/instances',
          label: navLabel('nav-workflow-instances', 'Tất cả hồ sơ'),
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/workflow/instances'),
        } : null,
        hasPermission('APPROVAL_AUTHORITY_MANAGE') ? {
          key: '/workflow/authorities',
          label: navLabel('nav-workflow-authorities', 'Thẩm quyền phê duyệt'),
          icon: <SafetyCertificateOutlined />,
          onClick: () => navigate('/workflow/authorities'),
        } : null,
      ].filter(Boolean),
    },
    hasPermission('SECURITY_ADMIN_MANAGE') || hasPermission('SECURITY_ACCOUNT_MANAGE') || hasPermission('SECURITY_AUDIT_VIEW') ? {
      key: 'security-group',
      icon: <SafetyOutlined />,
      label: navLabel('nav-security-group', 'Bảo mật'),
      children: [
        hasPermission('SECURITY_ACCOUNT_MANAGE') ? {
          key: '/security/accounts',
          label: navLabel('nav-security-accounts', 'Tài khoản'),
          icon: <UserOutlined />,
          onClick: () => navigate('/security/accounts'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE') ? {
          key: '/security/permissions/assignments',
          label: navLabel('nav-security-permissions-assignments', 'Phân quyền'),
          icon: <KeyOutlined />,
          onClick: () => navigate('/security/permissions/assignments'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE') ? {
          key: '/security/roles',
          label: navLabel('nav-security-roles', 'Vai trò'),
          icon: <IdcardOutlined />,
          onClick: () => navigate('/security/roles'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE') ? {
          key: '/security/admin-groups',
          label: navLabel('nav-security-admin-groups', 'Nhóm quản trị'),
          icon: <LockOutlined />,
          onClick: () => navigate('/security/admin-groups'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE') ? {
          key: '/security/departments/permissions',
          label: navLabel('nav-security-departments-permissions', 'Quyền phòng ban'),
          icon: <BankOutlined />,
          onClick: () => navigate('/security/departments/permissions'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE') ? {
          key: '/security/effective-permissions',
          label: navLabel('nav-security-effective-permissions', 'Kiểm tra quyền'),
          icon: <SafetyOutlined />,
          onClick: () => navigate('/security/effective-permissions'),
        } : null,
        hasPermission('SECURITY_AUDIT_VIEW') ? {
          key: '/security/audit',
          label: navLabel('nav-security-audit', 'Nhật ký kiểm toán'),
          icon: <AuditOutlined />,
          onClick: () => navigate('/security/audit'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('ORGANIZATION_COMPANY_MANAGE') || hasPermission('ORGANIZATION_DEPARTMENT_MANAGE') || hasPermission('ORGANIZATION_USER_MANAGE') ? {
      key: 'org-group',
      icon: <BankOutlined />,
      label: navLabel('nav-org-group', 'Tổ chức'),
      children: [
        hasPermission('ORGANIZATION_COMPANY_MANAGE') ? {
          key: '/organizations/companies',
          label: navLabel('nav-org-companies', 'Công ty'),
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/organizations/companies'),
        } : null,
        hasPermission('ORGANIZATION_DEPARTMENT_MANAGE') ? {
          key: '/organizations/departments',
          label: navLabel('nav-org-departments', 'Phòng ban'),
          icon: <BankOutlined />,
          onClick: () => navigate('/organizations/departments'),
        } : null,
        hasPermission('ORGANIZATION_USER_MANAGE') ? {
          key: '/organizations/users',
          label: navLabel('nav-org-users', 'Người dùng'),
          icon: <UserOutlined />,
          onClick: () => navigate('/organizations/users'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('TAG_MANAGE') ? {
      key: '/tags',
      icon: <TagsOutlined />,
      label: navLabel('nav-tags', 'Quản lý thẻ'),
      onClick: () => navigate('/tags'),
    } : null,
    hasPermission('SYSTEM_SETTING_MANAGE') ? {
      key: '/system/storage',
      icon: <SettingOutlined />,
      label: navLabel('nav-system-storage', 'Cấu hình lưu trữ'),
      onClick: () => navigate('/system/storage'),
    } : null,
    {
      key: '/system-health',
      icon: <SettingOutlined />,
      label: navLabel('nav-system-health', 'Hệ thống'),
      onClick: () => navigate('/system-health'),
    },
  ].filter(Boolean) as MenuProps['items'];

  const selectedKeys = [location.pathname];

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'user-info',
      label: user?.displayName || user?.username || 'User',
      disabled: true,
      style: { fontWeight: 600 },
    },
    { type: 'divider' },
    {
      key: 'profile',
      icon: <IdcardOutlined />,
      label: navLabel('nav-profile', 'Trang cá nhân'),
      onClick: () => navigate('/profile'),
    },
    {
      key: 'change-password',
      icon: <KeyOutlined />,
      label: navLabel('nav-change-password', 'Đổi mật khẩu'),
      onClick: () => navigate('/change-password'),
    },
    { type: 'divider' },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: navLabel('nav-logout', 'Đăng xuất'),
      onClick: handleLogout,
      danger: true,
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh', flexDirection: 'row' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        width={230}
        collapsedWidth={60}
        style={{
          overflow: 'auto',
          height: '100vh',
          position: 'sticky',
          top: 0,
          left: 0,
          fontSize: 15,
        }}
        theme="dark"
      >
        <div style={{
          height: 56,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderBottom: '1px solid rgba(255,255,255,0.1)',
        }}>
          <Typography.Title level={4} style={{ color: '#fff', margin: 0, whiteSpace: 'nowrap' }}>
            {collapsed ? 'PT' : 'PTKD ERP'}
          </Typography.Title>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={selectedKeys}
          openKeys={openKeys}
          onOpenChange={handleOpenChange}
          items={menuItems}
          style={{ borderRight: 0 }}
          data-testid="sidebar-menu"
        />
      </Sider>

      <Layout style={{ background: token.colorBgContainer }}>
        <Header style={{
          padding: '0 2%',
          background: token.colorBgContainer,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          boxShadow: '0 1px 4px rgba(0,0,0,0.08)',
          position: 'sticky',
          top: 0,
          zIndex: 99,
          height: 56,
        }}>
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            style={{ fontSize: 16 }}
            data-testid="sider-toggle"
          />
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            {companies.length > 0 && (
              <Select
                value={currentCompanyId}
                onChange={switchCompany}
                style={{ minWidth: 180 }}
                data-testid="company-selector"
                options={companies.map(c => ({ label: c.companyName, value: c.companyId }))}
                variant="borderless"
              />
            )}
            <Badge count={pendingCount} size="small" overflowCount={99}>
              <Button
                type="text"
                icon={<BellOutlined style={{ fontSize: 18 }} />}
                onClick={() => navigate('/workflow/my-approvals')}
                title="Việc chờ duyệt"
                data-testid="notification-bell"
              />
            </Badge>
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
              <div style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 8 }}>
                <Avatar icon={<UserOutlined />} style={{ backgroundColor: token.colorPrimary }} />
                {!collapsed && (
                  <Typography.Text strong>
                    {user?.displayName || user?.username}
                  </Typography.Text>
                )}
              </div>
            </Dropdown>
          </div>
        </Header>

        <Content style={{ padding: '16px 24px', width: '100%' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
};

export default AuthenticatedShell;
