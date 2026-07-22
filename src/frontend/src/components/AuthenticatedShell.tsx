import React from 'react';
import { Button, Layout, Menu, Typography, Select } from 'antd';
import { useCompany } from '../auth/CompanyProvider';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth, usePermissions } from '../auth/AuthProvider';

const { Header, Content, Footer } = Layout;
const { Title } = Typography;

/**
 * AuthenticatedShell — minimal authenticated layout placeholder (Phase 1B.1-J DEC-1B-J-04).
 * Provides logout button and a content area rendered via <Outlet />.
 * Admin navigation menus are deferred to the Security Admin UI phase.
 */
const AuthenticatedShell: React.FC = () => {
  const { logout, user } = useAuth();
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const { companies, currentCompanyId, switchCompany } = useCompany();

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <Layout className="layout" style={{ minHeight: '100vh' }}>
      <Header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
          <Title
            level={5}
            style={{ color: '#fff', margin: 0 }}
          >
            PTKD ERP
          </Title>
          <Menu
            theme="dark"
            mode="horizontal"
            style={{ flex: 1, background: 'transparent' }}
            selectedKeys={[]}
          >
            <Menu.Item key="home">
              <Link to="/">Home</Link>
            </Menu.Item>
            <Menu.Item key="system-health">
              <Link to="/system-health">System Health</Link>
            </Menu.Item>
            {hasPermission('SECURITY_ACCOUNT_MANAGE', 'GLOBAL') && (
              <Menu.Item key="security-accounts" data-testid="nav-account-management">
                <Link to="/security/accounts">Account Management</Link>
              </Menu.Item>
            )}
            {hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') && (
              <Menu.Item key="security-permissions" data-testid="nav-permission-assignment">
                <Link to="/security/permissions/assignments">Permission Assignment</Link>
              </Menu.Item>
            )}
            {hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') && (
              <Menu.Item key="security-roles" data-testid="nav-role-management">
                <Link to="/security/roles">Role Management</Link>
              </Menu.Item>
            )}
            {hasPermission('SECURITY_AUDIT_VIEW', 'GLOBAL') && (
              <Menu.Item key="security-audit" data-testid="nav-audit-viewer">
                <Link to="/security/audit">Audit Viewer</Link>
              </Menu.Item>
            )}
          </Menu>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {companies.length > 0 && (
            <Select
              value={currentCompanyId}
              onChange={switchCompany}
              style={{ width: 200 }}
              data-testid="company-selector"
              options={companies.map(c => ({ label: c.companyName, value: c.companyId }))}
            />
          )}
          {user?.displayName && (
            <Typography.Text style={{ color: '#fff' }}>
              {user.displayName}
            </Typography.Text>
          )}
          {!user?.displayName && user?.username && (
            <Typography.Text style={{ color: '#fff' }}>
              {user.username}
            </Typography.Text>
          )}
          <Button
            type="default"
            onClick={handleLogout}
            data-testid="logout-button"
          >
            Logout
          </Button>
        </div>
      </Header>

      <Content style={{ padding: '0 50px', marginTop: 20 }}>
        <div
          className="site-layout-content"
          style={{ padding: 24, minHeight: 380, background: '#fff' }}
        >
          <Outlet />
        </div>
      </Content>

      <Footer style={{ textAlign: 'center' }}>PTKD ERP ©2026</Footer>
    </Layout>
  );
};

export default AuthenticatedShell;
