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
            {hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') && (
              <Menu.Item key="security-admin-groups" data-testid="nav-admin-group-management">
                <Link to="/security/admin-groups">Admin Group Management</Link>
              </Menu.Item>
            )}
            {hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') && (
              <Menu.Item key="security-department-permissions" data-testid="nav-department-permissions">
                <Link to="/security/departments/permissions">Department Permissions</Link>
              </Menu.Item>
            )}
            {hasPermission('SECURITY_ADMIN_MANAGE', 'GLOBAL') && (
              <Menu.Item key="security-effective-permissions" data-testid="nav-effective-permissions">
                <Link to="/security/effective-permissions">Effective Permissions</Link>
              </Menu.Item>
            )}
            {hasPermission('SECURITY_AUDIT_VIEW', 'GLOBAL') && (
              <Menu.Item key="security-audit" data-testid="nav-audit-viewer">
                <Link to="/security/audit">Audit Viewer</Link>
              </Menu.Item>
            )}
            {hasPermission('CUSTOMER_VIEW_BASIC', 'GLOBAL') && (
              <Menu.Item key="customers" data-testid="nav-customers">
                <Link to="/customers">Customers</Link>
              </Menu.Item>
            )}
            {hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL') && (
              <Menu.Item key="customer-proposals" data-testid="nav-customer-proposals">
                <Link to="/customers/proposals">My Proposals</Link>
              </Menu.Item>
            )}
            {hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL') && (
              <Menu.Item key="customer-change-requests" data-testid="nav-customer-change-requests">
                <Link to="/customers/change-requests">My Change Requests</Link>
              </Menu.Item>
            )}

            {hasPermission('CUSTOMER_MERGE_REQUEST_VIEW', 'GLOBAL') && (
              <Menu.Item key="merge-requests" data-testid="nav-merge-requests">
                <Link to="/customers/merge-requests">Merge Requests</Link>
              </Menu.Item>
            )}
            {hasPermission('CUSTOMER_MERGE_REQUEST_CREATE', 'GLOBAL') && (
              <Menu.Item key="find-duplicates" data-testid="nav-find-duplicates">
                <Link to="/customers/merge/search">Find Duplicates</Link>
              </Menu.Item>
            )}

            {/* Payments & Reconciliation */}
            {hasPermission('PAYMENT_CREATE_DRAFT', 'GLOBAL') && (
              <Menu.Item key="payments" data-testid="nav-payments">
                <Link to="/payments">Payments</Link>
              </Menu.Item>
            )}
            {hasPermission('RECONCILIATION_PREPARE', 'GLOBAL') && (
              <Menu.Item key="reconciliation-daily" data-testid="nav-reconciliation-daily">
                <Link to="/reconciliation/daily">Daily Reconciliation</Link>
              </Menu.Item>
            )}
            {hasPermission('RECONCILIATION_PREPARE', 'GLOBAL') && (
              <Menu.Item key="reconciliation-monthly" data-testid="nav-reconciliation-monthly">
                <Link to="/reconciliation/monthly">Monthly Reconciliation</Link>
              </Menu.Item>
            )}

            {hasPermission('WORKFLOW_DEFINITION_VIEW', 'GLOBAL') && (
              <Menu.Item key="service-types" data-testid="nav-service-types">
                <Link to="/services/types">Service Types</Link>
              </Menu.Item>
            )}
            {hasPermission('SERVICE_VIEW', 'COMPANY') && (
              <Menu.Item key="services" data-testid="nav-services">
                <Link to="/services">Services</Link>
              </Menu.Item>
            )}

            <Menu.Item key="my-approvals" data-testid="nav-my-approvals">
              <Link to="/workflow/my-approvals">My Approvals</Link>
            </Menu.Item>
            <Menu.Item key="my-requests" data-testid="nav-my-requests">
              <Link to="/workflow/my-requests">My Requests</Link>
            </Menu.Item>
            {hasPermission('WORKFLOW_VIEW', 'GLOBAL') && (
              <Menu.Item key="workflow" data-testid="nav-workflow">
                <Link to="/workflow">Workflow Admin</Link>
              </Menu.Item>
            )}
            {hasPermission('WORKFLOW_VIEW', 'GLOBAL') && (
              <Menu.Item key="workflow-bindings" data-testid="nav-workflow-bindings">
                <Link to="/workflow/bindings">Workflow Bindings</Link>
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
