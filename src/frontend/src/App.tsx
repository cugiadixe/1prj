import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider, useAuth } from './auth/AuthProvider';
import { CompanyProvider } from './auth/CompanyProvider';
import ProtectedRoute from './components/ProtectedRoute';
import AuthenticatedShell from './components/AuthenticatedShell';
import LoginPage from './pages/LoginPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import Home from './pages/Home';
import SystemHealth from './pages/SystemHealth';
import AccountManagementPage from './pages/AccountManagementPage';
import AccountDetailPage from './pages/AccountDetailPage';
import PermissionAssignmentPage from './permissionAssignment/PermissionAssignmentPage';
import AuditViewerPage from './auditViewer/AuditViewerPage';
import RoleManagementPage from './roleManagement/RoleManagementPage';
import UserRoleAssignmentsPage from './userRoleAssignments/UserRoleAssignmentsPage';
import AdminGroupManagementPage from './adminGroupManagement/AdminGroupManagementPage';
import UserAdminGroupAssignmentsPage from './userAdminGroupAssignments/UserAdminGroupAssignmentsPage';
import DepartmentPermissionsPage from './departmentPermissions/DepartmentPermissionsPage';
import EffectivePermissionDiagnosticsPage from './effectivePermissionDiagnostics/EffectivePermissionDiagnosticsPage';
import CustomersPage from './customers/CustomersPage';
import CustomerDetailPage from './customers/CustomerDetailPage';
import CustomerCreatePage from './customers/CustomerCreatePage';
import CustomerEditPage from './customers/CustomerEditPage';
import WorkflowDefinitionsPage from './workflow/WorkflowDefinitionsPage';
import WorkflowDefinitionCreatePage from './workflow/WorkflowDefinitionCreatePage';
import WorkflowDefinitionDetailPage from './workflow/WorkflowDefinitionDetailPage';
import WorkflowDefinitionEditPage from './workflow/WorkflowDefinitionEditPage';
import WorkflowVersionCreatePage from './workflow/WorkflowVersionCreatePage';
import WorkflowVersionDetailPage from './workflow/WorkflowVersionDetailPage';
import WorkflowBindingsPage from './workflow/WorkflowBindingsPage';
import WorkflowMyApprovalsPage from './workflow/WorkflowMyApprovalsPage';
import WorkflowMyRequestsPage from './workflow/WorkflowMyRequestsPage';
import WorkflowInstanceDetailPage from './workflow/WorkflowInstanceDetailPage';
import CustomerProposalCreatePage from './customers/CustomerProposalCreatePage';
import CustomerProposalDetailPage from './customers/CustomerProposalDetailPage';
import CustomerMyProposalsPage from './customers/CustomerMyProposalsPage';
import CustomerMasterChangeRequestsPage from './customers/CustomerMasterChangeRequestsPage';
import CustomerMasterChangeRequestDetailPage from './customers/CustomerMasterChangeRequestDetailPage';
import CustomerMergeDuplicateSearchPage from './customers/CustomerMergeDuplicateSearchPage';
import CustomerMergeRequestCreatePage from './customers/CustomerMergeRequestCreatePage';
import CustomerMergeRequestsPage from './customers/CustomerMergeRequestsPage';
import CustomerMergeRequestDetailPage from './customers/CustomerMergeRequestDetailPage';
import GravesPage from './graves/GravesPage';
import GraveDetailPage from './graves/GraveDetailPage';
import GraveCreatePage from './graves/GraveCreatePage';
import GraveEditPage from './graves/GraveEditPage';
import CardReprintRequestsPage from './cards/CardReprintRequestsPage';
import CardReprintRequestCreatePage from './cards/CardReprintRequestCreatePage';
import CardReprintRequestDetailPage from './cards/CardReprintRequestDetailPage';
import PaymentListPage from './payments/pages/PaymentListPage';
import PaymentDetailPage from './payments/pages/PaymentDetailPage';
import PaymentCreatePage from './payments/pages/PaymentCreatePage';
import ReconciliationDailyPage from './payments/pages/ReconciliationDailyPage';
import ReconciliationMonthlyPage from './payments/pages/ReconciliationMonthlyPage';
import ServiceTypeListPage from './services/ServiceTypeListPage';
import ServiceTypeDetailPage from './services/ServiceTypeDetailPage';
import ServiceTypeFormPage from './services/ServiceTypeFormPage';
import ServiceListPage from './services/ServiceListPage';
import ServiceDetailPage from './services/ServiceDetailPage';
import ServiceCreatePage from './services/ServiceCreatePage';
import CarePackageRequestsPage from './care-packages/CarePackageRequestsPage';
import CarePackageRequestCreatePage from './care-packages/CarePackageRequestCreatePage';
import CarePackageRequestDetailPage from './care-packages/CarePackageRequestDetailPage';
import TagManagementPage from './tags/TagManagementPage';
const queryClient = new QueryClient();

/**
 * ChangePasswordGuard — ensures /change-password is accessible only to
 * authenticated users who are required to change their password.
 * Redirects otherwise (DEC-1B-J-03).
 */
const ChangePasswordGuard: React.FC = () => {
  const { isAuthenticated, mustChangePassword, isBootstrapping } = useAuth();

  if (isBootstrapping) return null;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!mustChangePassword) return <Navigate to="/" replace />;

  return <ChangePasswordPage />;
};

const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <AuthProvider>
          <CompanyProvider>
          <Routes>
            {/* Public routes */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/change-password" element={<ChangePasswordGuard />} />

            {/* Authenticated shell — wraps protected pages */}
            <Route
              element={
                <ProtectedRoute>
                  <AuthenticatedShell />
                </ProtectedRoute>
              }
            >
              <Route index element={<Home />} />
              <Route path="system-health" element={<SystemHealth />} />
              {/* Phase 1B.1-K — Account Management UI */}
              <Route path="security/accounts" element={<AccountManagementPage />} />
              <Route path="security/accounts/:accountId" element={<AccountDetailPage />} />
              {/* Phase 1B.1-N — Permission Assignment UI */}
              <Route path="security/permissions/assignments" element={<PermissionAssignmentPage />} />
              {/* Phase 1B.1-O — Audit Viewer UI */}
              <Route path="security/audit" element={<AuditViewerPage />} />
              {/* Phase 1B.1-P1 — Role Management UI */}
              <Route path="security/roles" element={<RoleManagementPage />} />
              {/* Phase 1B.1-Q1 — User Role Assignments UI */}
              <Route path="security/users/:userId/role-assignments" element={<UserRoleAssignmentsPage />} />
              {/* Phase 1B.1-P2 — Admin Group Management UI */}
              <Route path="security/admin-groups" element={<AdminGroupManagementPage />} />
              {/* Phase 1B.1-Q2 — User Admin Group Memberships UI */}
              <Route path="security/users/:userId/admin-group-assignments" element={<UserAdminGroupAssignmentsPage />} />
              {/* Phase 1B.1-R — Department Baseline Permission Management UI */}
              <Route path="security/departments/permissions" element={<DepartmentPermissionsPage />} />
              {/* Phase 1B.1-S — Effective Permission Diagnostics UI */}
              <Route path="security/effective-permissions" element={<EffectivePermissionDiagnosticsPage />} />
              {/* Phase 1B.2-B2 — Customer Frontend UI */}
              <Route path="customers" element={<CustomersPage />} />
              <Route path="customers/new" element={<CustomerCreatePage />} />
              <Route path="customers/:customerId" element={<CustomerDetailPage />} />
              <Route path="customers/:customerId/edit" element={<CustomerEditPage />} />
              <Route path="customers/proposals" element={<CustomerMyProposalsPage />} />
              <Route path="customers/proposals/new" element={<CustomerProposalCreatePage />} />
              <Route path="customers/proposals/:id" element={<CustomerProposalDetailPage />} />
              <Route path="customers/change-requests" element={<CustomerMasterChangeRequestsPage />} />
              <Route path="customers/change-requests/:id" element={<CustomerMasterChangeRequestDetailPage />} />
              {/* Phase 1B.5-C — Customer Merge UI */}
              <Route path="customers/merge/search" element={<CustomerMergeDuplicateSearchPage />} />
              <Route path="customers/merge/new" element={<CustomerMergeRequestCreatePage />} />
              <Route path="customers/merge-requests" element={<CustomerMergeRequestsPage />} />
              <Route path="customers/merge-requests/:id" element={<CustomerMergeRequestDetailPage />} />
              {/* Quản lý mộ */}
              <Route path="graves" element={<GravesPage />} />
              <Route path="graves/new" element={<GraveCreatePage />} />
              <Route path="graves/:graveId" element={<GraveDetailPage />} />
              <Route path="graves/:graveId/edit" element={<GraveEditPage />} />
              {/* Phase 1B.8-C — Card Reprint UI */}
              <Route path="cards/reprints" element={<CardReprintRequestsPage />} />
              <Route path="cards/reprints/new" element={<CardReprintRequestCreatePage />} />
              <Route path="cards/reprints/:id" element={<CardReprintRequestDetailPage />} />

              <Route path="payments" element={<PaymentListPage />} />
              <Route path="payments/new" element={<PaymentCreatePage />} />
              <Route path="payments/:id" element={<PaymentDetailPage />} />
              <Route path="reconciliation/daily" element={<ReconciliationDailyPage />} />
              <Route path="reconciliation/monthly" element={<ReconciliationMonthlyPage />} />

              {/* Phase 1B.3-B2 — Workflow Admin Configuration UI */}
              <Route path="workflow" element={<WorkflowDefinitionsPage />} />
              <Route path="workflow/definitions/new" element={<WorkflowDefinitionCreatePage />} />
              <Route path="workflow/definitions/:definitionId" element={<WorkflowDefinitionDetailPage />} />
              <Route path="workflow/definitions/:definitionId/edit" element={<WorkflowDefinitionEditPage />} />
              <Route path="workflow/definitions/:definitionId/versions/new" element={<WorkflowVersionCreatePage />} />
              <Route path="workflow/definitions/:definitionId/versions/:versionId" element={<WorkflowVersionDetailPage />} />
              <Route path="workflow/bindings" element={<WorkflowBindingsPage />} />
              {/* Phase 1B.3-B3 — Workflow Runtime / My Approvals UI */}
              <Route path="workflow/my-approvals" element={<WorkflowMyApprovalsPage />} />
              <Route path="workflow/my-requests" element={<WorkflowMyRequestsPage />} />
              <Route path="workflow/instances/:instanceId" element={<WorkflowInstanceDetailPage />} />
              {/* Phase 1B.6-C — Service Module UI */}
              <Route path="services/types" element={<ServiceTypeListPage />} />
              <Route path="services/types/new" element={<ServiceTypeFormPage />} />
              <Route path="services/types/:id" element={<ServiceTypeDetailPage />} />
              <Route path="services/types/:id/edit" element={<ServiceTypeFormPage />} />
              <Route path="services" element={<ServiceListPage />} />
              <Route path="services/new" element={<ServiceCreatePage />} />
              <Route path="services/:id" element={<ServiceDetailPage />} />
              {/* Phase 1B.9-C — Care Package Sales UI */}
              <Route path="care-packages" element={<CarePackageRequestsPage />} />
              <Route path="care-packages/new" element={<CarePackageRequestCreatePage />} />
              <Route path="care-packages/:id" element={<CarePackageRequestDetailPage />} />
              {/* Quản lý thẻ (hashtag) */}
              <Route path="tags" element={<TagManagementPage />} />
            </Route>

            {/* Catch-all */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </CompanyProvider>
        </AuthProvider>
      </Router>
    </QueryClientProvider>
  );
};

export default App;
