/**
 * PermissionAssignmentPage — Phase 1B.1-N.
 *
 * Security admin UI for managing individual user permission assignments.
 * Gate: SECURITY_ADMIN_MANAGE GLOBAL.
 * Supports GLOBAL and COMPANY scopes only (ENTITY deferred).
 * COMPANY assignment requires selected current company from Phase M CompanyProvider.
 * Backend remains authoritative — this is a frontend-only phase.
 */

import React, { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Form,
  Input,
  List,
  Modal,
  Select,
  Space,
  Spin,
  Tag,
  Typography,
} from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useCompany } from '../auth/CompanyProvider';
import {
  fetchPermissionCatalog,
  fetchUserIndividualPermissions,
  fetchEffectivePermissions,
  grantIndividualPermission,
  deactivateIndividualPermission,
} from './permissionAssignmentApi';
import type {
  PermissionDto,
  UserIndividualPermissionDto,
  CreateUserIndividualPermissionRequest,
} from './permissionAssignmentApi';
import {
  getAssignmentErrorMessage,
  isPermissionDenied,
  PERMISSION_DENIED_MSG,
  GENERIC_ERROR,
} from './errorMessages';
import { searchAccounts } from '../accountManagement/accountManagementApi';

const { Title, Text } = Typography;
const { Option } = Select;
const { Search } = Input;

// ── Grant Permission Modal ────────────────────────────────────────────────────

interface GrantModalProps {
  open: boolean;
  permissions: PermissionDto[];
  currentCompanyId: number | null;
  isLoading: boolean;
  errorMessage: string | null;
  onGrant: (request: CreateUserIndividualPermissionRequest) => void;
  onCancel: () => void;
}

const GrantPermissionModal: React.FC<GrantModalProps> = ({
  open,
  permissions,
  currentCompanyId,
  isLoading,
  errorMessage,
  onGrant,
  onCancel,
}) => {
  const [permissionCode, setPermissionCode] = useState<string | undefined>(undefined);
  const [scopeType, setScopeType] = useState<string>('GLOBAL');
  const [grantType, setGrantType] = useState<string>('ALLOW');
  const [reason, setReason] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleOk = () => {
    if (!permissionCode) {
      setValidationError('Please select a permission.');
      return;
    }
    if (scopeType === 'COMPANY' && currentCompanyId === null) {
      setValidationError('A company must be selected for company-scoped assignments. Please select a company from the header.');
      return;
    }
    setValidationError(null);
    const request: CreateUserIndividualPermissionRequest = {
      permissionCode,
      scopeType,
      companyId: scopeType === 'COMPANY' ? currentCompanyId : null,
      grantType,
      effectiveFrom: new Date().toISOString(),
      effectiveTo: null,
      reason: reason.trim() || null,
    };
    onGrant(request);
  };

  const handleCancel = () => {
    setPermissionCode(undefined);
    setScopeType('GLOBAL');
    setGrantType('ALLOW');
    setReason('');
    setValidationError(null);
    onCancel();
  };

  const activePermissions = permissions.filter(p => p.isActive);

  return (
    <Modal
      open={open}
      title="Grant Permission"
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={isLoading}
      okText="Grant"
      cancelText="Cancel"
      data-testid="grant-permission-modal"
      destroyOnHidden
    >
      <Form layout="vertical">
        <Form.Item label="Permission" required>
          <Select
            showSearch
            placeholder="Select a permission"
            value={permissionCode}
            onChange={setPermissionCode}
            filterOption={(input, option) =>
              (option?.label as string ?? '').toLowerCase().includes(input.toLowerCase())
            }
            options={activePermissions.map(p => ({
              label: `${p.permissionCode} — ${p.description ?? p.actionCode}`,
              value: p.permissionCode,
            }))}
            data-testid="permission-select"
            aria-label="Select permission"
          />
        </Form.Item>

        <Form.Item label="Scope">
          <Select
            value={scopeType}
            onChange={setScopeType}
            data-testid="scope-select"
            aria-label="Select scope"
          >
            <Option value="GLOBAL">GLOBAL</Option>
            <Option
              value="COMPANY"
              disabled={currentCompanyId === null}
            >
              COMPANY{currentCompanyId === null ? ' (select a company first)' : ''}
            </Option>
          </Select>
        </Form.Item>

        {scopeType === 'COMPANY' && currentCompanyId === null && (
          <Alert
            type="warning"
            message="Please select a company from the header to assign company-scoped permissions."
            data-testid="company-required-warning"
            style={{ marginBottom: 12 }}
          />
        )}

        <Form.Item label="Grant Type">
          <Select
            value={grantType}
            onChange={setGrantType}
            data-testid="grant-type-select"
            aria-label="Select grant type"
          >
            <Option value="ALLOW">ALLOW</Option>
            <Option value="DENY">DENY</Option>
          </Select>
        </Form.Item>

        <Form.Item label="Reason (optional)">
          <Input.TextArea
            rows={2}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            maxLength={500}
            placeholder="Enter reason (optional)"
            data-testid="grant-reason-input"
            aria-label="Reason"
          />
        </Form.Item>
      </Form>

      {validationError && (
        <Alert
          type="error"
          message={validationError}
          data-testid="grant-validation-error"
          style={{ marginTop: 8 }}
        />
      )}
      {errorMessage && (
        <Alert
          type="error"
          message={errorMessage}
          data-testid="grant-api-error"
          style={{ marginTop: 8 }}
        />
      )}
    </Modal>
  );
};

// ── Main Permission Assignment Page ───────────────────────────────────────────

const PermissionAssignmentPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();

  // User selection state
  const initialUserId = searchParams.get('userId');
  const [selectedUserId, setSelectedUserId] = useState<number | null>(
    initialUserId ? parseInt(initialUserId, 10) : null,
  );
  const [userSearch, setUserSearch] = useState('');
  const [showGrantModal, setShowGrantModal] = useState(false);
  const [grantError, setGrantError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // ── Account search query ────────────────────────────────────────────────────
  const {
    data: accountsData,
    isLoading: isLoadingAccounts,
    isError: isAccountsError,
    error: accountsError,
  } = useQuery({
    queryKey: ['permission-assignment-accounts', userSearch],
    queryFn: () => searchAccounts({ search: userSearch || undefined, page: 1, pageSize: 20 }),
    enabled: userSearch.length > 0,
    retry: false,
  });

  // ── Permission catalog query ────────────────────────────────────────────────
  const {
    data: catalog,
    isLoading: isLoadingCatalog,
    isError: isCatalogError,
    error: catalogError,
  } = useQuery({
    queryKey: ['permission-catalog'],
    queryFn: fetchPermissionCatalog,
    retry: false,
  });

  // ── Individual permissions for selected user ────────────────────────────────
  const {
    data: assignments,
    isLoading: isLoadingAssignments,
    isError: isAssignmentsError,
    error: assignmentsError,
  } = useQuery({
    queryKey: ['user-individual-permissions', selectedUserId],
    queryFn: () => fetchUserIndividualPermissions(selectedUserId!),
    enabled: selectedUserId !== null,
    retry: false,
  });

  // ── Effective permissions for selected user ─────────────────────────────────
  const {
    data: effectivePermissions,
    isLoading: isLoadingEffective,
    isError: isEffectiveError,
  } = useQuery({
    queryKey: ['user-effective-permissions', selectedUserId, currentCompanyId],
    queryFn: () => fetchEffectivePermissions(selectedUserId!, currentCompanyId),
    enabled: selectedUserId !== null,
    retry: false,
  });

  // ── Grant mutation ──────────────────────────────────────────────────────────
  const grantMutation = useMutation({
    mutationFn: (request: CreateUserIndividualPermissionRequest) =>
      grantIndividualPermission(selectedUserId!, request),
    onSuccess: () => {
      setShowGrantModal(false);
      setGrantError(null);
      setSuccessMessage('Permission granted successfully.');
      void queryClient.invalidateQueries({ queryKey: ['user-individual-permissions', selectedUserId] });
      void queryClient.invalidateQueries({ queryKey: ['user-effective-permissions', selectedUserId] });
    },
    onError: (err: unknown) => {
      setGrantError(getAssignmentErrorMessage(err));
    },
  });

  // ── Deactivate mutation ─────────────────────────────────────────────────────
  const deactivateMutation = useMutation({
    mutationFn: (assignment: UserIndividualPermissionDto) =>
      deactivateIndividualPermission(selectedUserId!, assignment.id, {
        rowVersion: assignment.rowVersion,
      }),
    onSuccess: () => {
      setSuccessMessage('Permission assignment revoked successfully.');
      void queryClient.invalidateQueries({ queryKey: ['user-individual-permissions', selectedUserId] });
      void queryClient.invalidateQueries({ queryKey: ['user-effective-permissions', selectedUserId] });
    },
    onError: (err: unknown) => {
      setSuccessMessage(null);
      Modal.error({
        title: 'Revoke Failed',
        content: getAssignmentErrorMessage(err),
      });
    },
  });

  const handleUserSearch = (value: string) => {
    setUserSearch(value);
    setSuccessMessage(null);
  };

  const handleSelectUser = (userId: number) => {
    setSelectedUserId(userId);
    setSuccessMessage(null);
  };

  const handleOpenGrant = () => {
    setGrantError(null);
    setShowGrantModal(true);
  };

  const handleGrantSubmit = (request: CreateUserIndividualPermissionRequest) => {
    grantMutation.mutate(request);
  };

  const handleGrantCancel = () => {
    setShowGrantModal(false);
    setGrantError(null);
    grantMutation.reset();
  };

  const handleDeactivate = (assignment: UserIndividualPermissionDto) => {
    setSuccessMessage(null);
    Modal.confirm({
      title: 'Revoke Permission Assignment',
      content: `Are you sure you want to revoke the ${assignment.grantType} assignment for ${assignment.permissionCode}?`,
      okText: 'Revoke',
      cancelText: 'Cancel',
      onOk: () => deactivateMutation.mutate(assignment),
    });
  };

  // ── Catalog error ───────────────────────────────────────────────────────────
  if (isCatalogError) {
    if (isPermissionDenied(catalogError)) {
      return (
        <div data-testid="permission-assignment-page">
          <Alert
            type="warning"
            message={PERMISSION_DENIED_MSG}
            data-testid="permission-denied-error"
          />
        </div>
      );
    }
    return (
      <div data-testid="permission-assignment-page">
        <Alert
          type="error"
          message={GENERIC_ERROR}
          data-testid="catalog-error"
        />
      </div>
    );
  }

  if (isLoadingCatalog) {
    return (
      <div style={{ textAlign: 'center', padding: 48 }} data-testid="permission-assignment-loading">
        <Spin size="large" />
      </div>
    );
  }

  const activeAssignments = (assignments ?? []).filter(
    a => a.assignmentStatus === 'ACTIVE',
  );

  return (
    <div data-testid="permission-assignment-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">
          ← Back
        </Button>
      </Space>

      <Title level={3}>Permission Assignment</Title>

      {successMessage && (
        <Alert
          type="success"
          message={successMessage}
          closable
          onClose={() => setSuccessMessage(null)}
          data-testid="success-message"
          style={{ marginBottom: 16 }}
        />
      )}

      {/* ── User/Account Selection ───────────────────────────────────────── */}
      <Card
        title="Select User"
        style={{ marginBottom: 16 }}
        data-testid="user-selection-card"
      >
        <Search
          placeholder="Search by username, employee code, or name"
          allowClear
          onSearch={handleUserSearch}
          style={{ width: 400, marginBottom: 12 }}
          data-testid="user-search-input"
          aria-label="Search users"
        />

        {isLoadingAccounts && <Spin data-testid="user-search-loading" />}

        {isAccountsError && (
          <Alert
            type="error"
            message={isPermissionDenied(accountsError) ? PERMISSION_DENIED_MSG : GENERIC_ERROR}
            data-testid="user-search-error"
          />
        )}

        {accountsData && accountsData.items.length > 0 && (
          <List
            size="small"
            dataSource={accountsData.items}
            data-testid="user-search-results"
            renderItem={(account) => (
              <List.Item
                key={account.accountId}
                actions={[
                  <Button
                    key="select"
                    type="link"
                    size="small"
                    onClick={() => handleSelectUser(account.userId)}
                    data-testid={`select-user-${account.userId}`}
                  >
                    Select
                  </Button>,
                ]}
              >
                <List.Item.Meta
                  title={`${account.username} — ${account.fullName ?? ''}`}
                  description={`User ID: ${account.userId} | Employee: ${account.employeeCode ?? '—'}`}
                />
              </List.Item>
            )}
          />
        )}

        {accountsData && accountsData.items.length === 0 && (
          <Text type="secondary" data-testid="user-search-empty">No users found.</Text>
        )}

        {selectedUserId !== null && (
          <Alert
            type="info"
            message={`Selected User ID: ${selectedUserId}`}
            data-testid="selected-user-info"
            style={{ marginTop: 8 }}
          />
        )}
      </Card>

      {/* ── Individual Assignments ────────────────────────────────────────── */}
      {selectedUserId !== null && (
        <Card
          title="Individual Permission Assignments"
          style={{ marginBottom: 16 }}
          extra={
            <Button
              type="primary"
              onClick={handleOpenGrant}
              data-testid="grant-permission-button"
            >
              Grant Permission
            </Button>
          }
          data-testid="assignments-card"
        >
          {isLoadingAssignments && (
            <div style={{ textAlign: 'center', padding: 24 }} data-testid="assignments-loading">
              <Spin />
            </div>
          )}

          {isAssignmentsError && (
            <Alert
              type="error"
              message={isPermissionDenied(assignmentsError) ? PERMISSION_DENIED_MSG : GENERIC_ERROR}
              data-testid="assignments-error"
            />
          )}

          {!isLoadingAssignments && !isAssignmentsError && activeAssignments.length === 0 && (
            <Text type="secondary" data-testid="no-assignments">
              No active individual permission assignments.
            </Text>
          )}

          {!isLoadingAssignments && !isAssignmentsError && activeAssignments.length > 0 && (
            <Descriptions
              bordered
              size="small"
              column={1}
              data-testid="assignments-list"
            >
              {activeAssignments.map((a) => (
                <Descriptions.Item
                  key={a.id}
                  label={
                    <Space>
                      <Text strong>{a.permissionCode}</Text>
                      <Tag color={a.grantType === 'ALLOW' ? 'green' : 'red'}>
                        {a.grantType}
                      </Tag>
                      <Tag>{a.scopeType}</Tag>
                      {a.scopeType === 'COMPANY' && a.companyId && (
                        <Tag color="blue">Company: {a.companyId}</Tag>
                      )}
                    </Space>
                  }
                >
                  <Space>
                    {a.reason && <Text type="secondary">Reason: {a.reason}</Text>}
                    <Button
                      danger
                      size="small"
                      onClick={() => handleDeactivate(a)}
                      loading={deactivateMutation.isPending}
                      data-testid={`revoke-${a.id}`}
                    >
                      Revoke
                    </Button>
                  </Space>
                </Descriptions.Item>
              ))}
            </Descriptions>
          )}
        </Card>
      )}

      {/* ── Effective Permissions (read-only) ─────────────────────────────── */}
      {selectedUserId !== null && (
        <Card
          title="Effective Permissions (Read-Only)"
          style={{ marginBottom: 16 }}
          data-testid="effective-permissions-card"
        >
          {isLoadingEffective && (
            <div style={{ textAlign: 'center', padding: 24 }} data-testid="effective-loading">
              <Spin />
            </div>
          )}

          {isEffectiveError && (
            <Alert
              type="warning"
              message="Unable to load effective permissions."
              data-testid="effective-error"
            />
          )}

          {!isLoadingEffective && !isEffectiveError && effectivePermissions && (
            <>
              {effectivePermissions.companyId !== null && (
                <Alert
                  type="info"
                  message={`Showing effective permissions for company ${effectivePermissions.companyId}. DENY-wins behavior is enforced by the backend.`}
                  data-testid="effective-company-info"
                  style={{ marginBottom: 8 }}
                />
              )}
              {effectivePermissions.companyId === null && (
                <Alert
                  type="info"
                  message="Showing global effective permissions. DENY-wins behavior is enforced by the backend."
                  data-testid="effective-global-info"
                  style={{ marginBottom: 8 }}
                />
              )}
              {effectivePermissions.permissionCodes.length === 0 && (
                <Text type="secondary" data-testid="no-effective-permissions">
                  No effective permissions.
                </Text>
              )}
              {effectivePermissions.permissionCodes.length > 0 && (
                <Space wrap data-testid="effective-permissions-list">
                  {effectivePermissions.permissionCodes.map((code) => (
                    <Tag key={code} color="blue">
                      {code}
                    </Tag>
                  ))}
                </Space>
              )}
            </>
          )}
        </Card>
      )}

      {/* ── Grant Modal ───────────────────────────────────────────────────── */}
      <GrantPermissionModal
        open={showGrantModal}
        permissions={catalog ?? []}
        currentCompanyId={currentCompanyId}
        isLoading={grantMutation.isPending}
        errorMessage={grantError}
        onGrant={handleGrantSubmit}
        onCancel={handleGrantCancel}
      />
    </div>
  );
};

export default PermissionAssignmentPage;
