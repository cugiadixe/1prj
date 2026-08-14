import React, { useState } from 'react';
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

const AuthenticatedShell: React.FC = () => {
  const { logout, user } = useAuth();
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const location = useLocation();
  const { companies, currentCompanyId, switchCompany } = useCompany();
  const [collapsed, setCollapsed] = useState(false);
  const { token } = theme.useToken();

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
      label: 'Trang chủ',
      onClick: () => navigate('/'),
    },
    hasPermission('CUSTOMER_VIEW_BASIC', 'GLOBAL') ? {
      key: 'customers-group',
      icon: <TeamOutlined />,
      label: 'Khách hàng',
      children: [
        {
          key: '/customers',
          label: 'Danh sách KH',
          icon: <TeamOutlined />,
          onClick: () => navigate('/customers'),
        },
        hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL') ? {
          key: '/customers/proposals',
          label: 'Đề xuất KH mới',
          icon: <FileSearchOutlined />,
          onClick: () => navigate('/customers/proposals'),
        } : null,
        hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL') ? {
          key: '/customers/change-requests',
          label: 'Yêu cầu thay đổi',
          icon: <SwapOutlined />,
          onClick: () => navigate('/customers/change-requests'),
        } : null,
        hasPermission('CUSTOMER_MERGE_REQUEST_VIEW', 'GLOBAL') ? {
          key: '/customers/merge-requests',
          label: 'Gộp khách hàng',
          icon: <SwapOutlined />,
          onClick: () => navigate('/customers/merge-requests'),
        } : null,
        hasPermission('CUSTOMER_MERGE_REQUEST_CREATE', 'GLOBAL') ? {
          key: '/customers/merge/search',
          label: 'Tìm trùng lặp',
          icon: <FileSearchOutlined />,
          onClick: () => navigate('/customers/merge/search'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('GRAVE_VIEW', 'GLOBAL') ? {
      key: '/graves',
      icon: <EnvironmentOutlined />,
      label: 'Quản lý mộ',
      onClick: () => navigate('/graves'),
    } : null,
    hasPermission('PAYMENT_CREATE_DRAFT', 'GLOBAL') || hasPermission('RECONCILIATION_PREPARE', 'GLOBAL') ? {
      key: 'payments-group',
      icon: <DollarOutlined />,
      label: 'Thanh toán',
      children: [
        hasPermission('PAYMENT_CREATE_DRAFT', 'GLOBAL') ? {
          key: '/payments',
          label: 'Giao dịch',
          icon: <DollarOutlined />,
          onClick: () => navigate('/payments'),
        } : null,
        hasPermission('RECONCILIATION_PREPARE', 'GLOBAL') ? {
          key: '/reconciliation/daily',
          label: 'Đối soát ngày',
          icon: <ReconciliationOutlined />,
          onClick: () => navigate('/reconciliation/daily'),
        } : null,
        hasPermission('RECONCILIATION_PREPARE', 'GLOBAL') ? {
          key: '/reconciliation/monthly',
          label: 'Đối soát tháng',
          icon: <ReconciliationOutlined />,
          onClick: () => navigate('/reconciliation/monthly'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL') || hasPermission('SERVICE_VIEW', 'COMPANY') ? {
      key: 'services-group',
      icon: <ToolOutlined />,
      label: 'Dịch vụ',
      children: [
        hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL') ? {
          key: '/services/types',
          label: 'Gói dịch vụ',
          icon: <SettingOutlined />,
          onClick: () => navigate('/services/types'),
        } : null,
        hasPermission('SERVICE_VIEW', 'COMPANY') ? {
          key: '/services',
          label: 'Bảng tổng hợp dịch vụ',
          icon: <ToolOutlined />,
          onClick: () => navigate('/services'),
        } : null,
        {
          key: '/care-packages',
          label: 'Gói chăm sóc',
          icon: <HeartOutlined />,
          onClick: () => navigate('/care-packages'),
        },
      ].filter(Boolean),
    } : null,
    {
      key: '/cards/reprints',
      icon: <CreditCardOutlined />,
      label: 'In lại thẻ',
      onClick: () => navigate('/cards/reprints'),
    },
    {
      key: 'workflow-group',
      icon: <ApartmentOutlined />,
      label: 'Quy trình',
      children: [
        {
          key: '/workflow/my-approvals',
          label: 'Chờ duyệt',
          icon: <CheckSquareOutlined />,
          onClick: () => navigate('/workflow/my-approvals'),
        },
        {
          key: '/workflow/my-requests',
          label: 'Yêu cầu của tôi',
          icon: <AuditOutlined />,
          onClick: () => navigate('/workflow/my-requests'),
        },
        hasPermission('WORKFLOW_VIEW', 'GLOBAL') ? {
          key: '/workflow',
          label: 'Quản trị quy trình',
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/workflow'),
        } : null,
        hasPermission('WORKFLOW_VIEW', 'GLOBAL') ? {
          key: '/workflow/bindings',
          label: 'Liên kết quy trình',
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/workflow/bindings'),
        } : null,
        hasPermission('WORKFLOW_VIEW', 'GLOBAL') ? {
          key: '/workflow/instances',
          label: 'Tất cả hồ sơ',
          icon: <ApartmentOutlined />,
          onClick: () => navigate('/workflow/instances'),
        } : null,
        hasPermission('APPROVAL_AUTHORITY_MANAGE', 'GLOBAL') ? {
          key: '/workflow/authorities',
          label: 'Thẩm quyền phê duyệt',
          icon: <SafetyCertificateOutlined />,
          onClick: () => navigate('/workflow/authorities'),
        } : null,
      ].filter(Boolean),
    },
    hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') || hasPermission('SECURITY_ACCOUNT_MANAGE', 'GLOBAL') || hasPermission('SECURITY_AUDIT_VIEW', 'GLOBAL') ? {
      key: 'security-group',
      icon: <SafetyOutlined />,
      label: 'Bảo mật',
      children: [
        hasPermission('SECURITY_ACCOUNT_MANAGE', 'GLOBAL') ? {
          key: '/security/accounts',
          label: 'Tài khoản',
          icon: <UserOutlined />,
          onClick: () => navigate('/security/accounts'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') ? {
          key: '/security/permissions/assignments',
          label: 'Phân quyền',
          icon: <KeyOutlined />,
          onClick: () => navigate('/security/permissions/assignments'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') ? {
          key: '/security/roles',
          label: 'Vai trò',
          icon: <IdcardOutlined />,
          onClick: () => navigate('/security/roles'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') ? {
          key: '/security/admin-groups',
          label: 'Nhóm quản trị',
          icon: <LockOutlined />,
          onClick: () => navigate('/security/admin-groups'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') ? {
          key: '/security/departments/permissions',
          label: 'Quyền phòng ban',
          icon: <BankOutlined />,
          onClick: () => navigate('/security/departments/permissions'),
        } : null,
        hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') ? {
          key: '/security/effective-permissions',
          label: 'Kiểm tra quyền',
          icon: <SafetyOutlined />,
          onClick: () => navigate('/security/effective-permissions'),
        } : null,
        hasPermission('SECURITY_AUDIT_VIEW', 'GLOBAL') ? {
          key: '/security/audit',
          label: 'Nhật ký kiểm toán',
          icon: <AuditOutlined />,
          onClick: () => navigate('/security/audit'),
        } : null,
      ].filter(Boolean),
    } : null,
    hasPermission('TAG_MANAGE', 'GLOBAL') ? {
      key: '/tags',
      icon: <TagsOutlined />,
      label: 'Quản lý thẻ',
      onClick: () => navigate('/tags'),
    } : null,
    {
      key: '/system-health',
      icon: <SettingOutlined />,
      label: 'Hệ thống',
      onClick: () => navigate('/system-health'),
    },
  ].filter(Boolean) as MenuProps['items'];

  const selectedKeys = [location.pathname];
  const openKeys = (() => {
    const path = location.pathname;
    if (path.startsWith('/customers')) return ['customers-group'];
    if (path.startsWith('/payments') || path.startsWith('/reconciliation')) return ['payments-group'];
    if (path.startsWith('/services') || path.startsWith('/care-packages')) return ['services-group'];
    if (path.startsWith('/workflow')) return ['workflow-group'];
    if (path.startsWith('/security')) return ['security-group'];
    return [];
  })();

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
      label: 'Trang cá nhân',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'change-password',
      icon: <KeyOutlined />,
      label: 'Đổi mật khẩu',
      onClick: () => navigate('/change-password'),
    },
    { type: 'divider' },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Đăng xuất',
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
          defaultOpenKeys={openKeys}
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
