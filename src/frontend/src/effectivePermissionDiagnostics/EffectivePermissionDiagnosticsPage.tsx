import React, { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Collapse,
  Descriptions,
  Input,
  List,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useCompany } from '../auth/CompanyProvider';
import { effectivePermissionDiagnosticsApi } from './effectivePermissionDiagnosticsApi';
import type {
  PermissionDto,
  UserRoleAssignmentDto,
  UserAdminGroupAssignmentDto,
} from './effectivePermissionDiagnosticsApi';
import { getSanitizedErrorMessage } from './errorMessages';

const { Title, Text } = Typography;

const EffectivePermissionDiagnosticsPage: React.FC = () => {
  const navigate = useNavigate();
  const { currentCompanyId } = useCompany();

  const [userIdInput, setUserIdInput] = useState('');
  const [submittedUserId, setSubmittedUserId] = useState<number | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleLookup = () => {
    const trimmed = userIdInput.trim();
    if (!trimmed) {
      setValidationError('User ID is required.');
      return;
    }
    const parsed = Number(trimmed);
    if (!Number.isInteger(parsed) || parsed <= 0) {
      setValidationError('User ID must be a positive integer.');
      return;
    }
    setValidationError(null);
    setSubmittedUserId(parsed);
  };

  const {
    data: effectivePermissions,
    isLoading: isLoadingEffective,
    isError: isEffectiveError,
    error: effectiveError,
  } = useQuery({
    queryKey: ['effective-permissions', submittedUserId, currentCompanyId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchEffectivePermissions(
        submittedUserId!,
        currentCompanyId,
      ),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const { data: catalog } = useQuery({
    queryKey: ['permission-catalog-diagnostics'],
    queryFn: effectivePermissionDiagnosticsApi.fetchPermissionCatalog,
    retry: false,
  });

  const {
    data: individualPermissions,
    isLoading: isLoadingIndividual,
  } = useQuery({
    queryKey: ['diagnostics-individual-permissions', submittedUserId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchUserIndividualPermissions(submittedUserId!),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const {
    data: roleAssignments,
    isLoading: isLoadingRoles,
  } = useQuery({
    queryKey: ['diagnostics-role-assignments', submittedUserId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchUserRoleAssignments(submittedUserId!),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const {
    data: adminGroupAssignments,
    isLoading: isLoadingAdminGroups,
  } = useQuery({
    queryKey: ['diagnostics-admin-group-assignments', submittedUserId],
    queryFn: () =>
      effectivePermissionDiagnosticsApi.fetchUserAdminGroupAssignments(submittedUserId!),
    enabled: submittedUserId !== null,
    retry: false,
  });

  const catalogMap = new Map<string, PermissionDto>();
  catalog?.forEach((p) => catalogMap.set(p.permissionCode, p));

  const enrichedPermissions = effectivePermissions?.permissionCodes.map((code) => {
    const detail = catalogMap.get(code);
    return {
      code,
      description: detail?.description ?? null,
      moduleCode: detail?.moduleCode ?? null,
      dataScope: detail?.dataScope ?? null,
      isActive: detail?.isActive ?? null,
    };
  });

  return (
    <div data-testid="effective-permission-diagnostics-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">
          ← Back
        </Button>
      </Space>

      <Title level={3}>Effective Permission Diagnostics</Title>
      <Text type="secondary" data-testid="page-description">
        Backend-authoritative final effective permissions for a user.
        Source-level attribution is not available.
      </Text>

      <Card title="User Selection" style={{ marginTop: 16, marginBottom: 16 }} data-testid="user-selection-card">
        <Space>
          <Input
            placeholder="Enter User ID"
            value={userIdInput}
            onChange={(e) => {
              setUserIdInput(e.target.value);
              setValidationError(null);
            }}
            onPressEnter={handleLookup}
            style={{ width: 200 }}
            data-testid="user-id-input"
          />
          <Button type="primary" onClick={handleLookup} data-testid="lookup-button">
            Look Up
          </Button>
        </Space>
        {validationError && (
          <Alert
            type="warning"
            message={validationError}
            style={{ marginTop: 8 }}
            data-testid="validation-error"
          />
        )}
        {currentCompanyId !== null && (
          <div style={{ marginTop: 8 }}>
            <Text type="secondary" data-testid="company-context-indicator">
              Company context: {currentCompanyId}
            </Text>
          </div>
        )}
        {currentCompanyId === null && submittedUserId !== null && (
          <div style={{ marginTop: 8 }}>
            <Text type="secondary" data-testid="global-context-indicator">
              Showing global effective permissions (no company selected).
            </Text>
          </div>
        )}
      </Card>

      {submittedUserId !== null && isLoadingEffective && (
        <Spin data-testid="effective-loading" />
      )}

      {isEffectiveError && (
        <Alert
          type="error"
          message={getSanitizedErrorMessage(effectiveError, 'Failed to fetch effective permissions.')}
          data-testid="effective-error"
          style={{ marginBottom: 16 }}
        />
      )}

      {effectivePermissions && !isEffectiveError && (
        <>
          <Card
            title="Backend-Authoritative Effective Permissions"
            style={{ marginBottom: 16 }}
            data-testid="effective-permissions-card"
          >
            <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="User ID">{effectivePermissions.userId}</Descriptions.Item>
              <Descriptions.Item label="Company ID">
                {effectivePermissions.companyId ?? 'Global'}
              </Descriptions.Item>
              <Descriptions.Item label="Total Permissions">
                {effectivePermissions.permissionCodes.length}
              </Descriptions.Item>
            </Descriptions>

            {effectivePermissions.permissionCodes.length === 0 ? (
              <Text type="secondary" data-testid="no-permissions-message">
                This user has no effective permissions
                {currentCompanyId !== null ? ' for the selected company' : ''}.
              </Text>
            ) : (
              <Table
                dataSource={enrichedPermissions}
                rowKey="code"
                size="small"
                pagination={false}
                data-testid="effective-permissions-table"
                columns={[
                  {
                    title: 'Permission Code',
                    dataIndex: 'code',
                    key: 'code',
                    render: (code: string) => <Text strong>{code}</Text>,
                  },
                  {
                    title: 'Description',
                    dataIndex: 'description',
                    key: 'description',
                    render: (desc: string | null) => desc ?? <Text type="secondary">—</Text>,
                  },
                  {
                    title: 'Module',
                    dataIndex: 'moduleCode',
                    key: 'moduleCode',
                    render: (mod: string | null) => mod ?? <Text type="secondary">—</Text>,
                  },
                  {
                    title: 'Scope',
                    dataIndex: 'dataScope',
                    key: 'dataScope',
                    render: (scope: string | null) =>
                      scope ? <Tag>{scope}</Tag> : <Text type="secondary">—</Text>,
                  },
                  {
                    title: 'Active',
                    dataIndex: 'isActive',
                    key: 'isActive',
                    render: (active: boolean | null) =>
                      active === null ? (
                        <Text type="secondary">—</Text>
                      ) : active ? (
                        <Tag color="green">Yes</Tag>
                      ) : (
                        <Tag color="red">No</Tag>
                      ),
                  },
                ]}
              />
            )}
          </Card>

          <Card
            title="Context Only — Not Authoritative Source Attribution"
            style={{ marginBottom: 16 }}
            data-testid="contextual-sections-card"
          >
            <Alert
              type="info"
              message="The sections below show contextual information from related APIs. They do not represent authoritative source-level attribution for the effective permissions above."
              style={{ marginBottom: 16 }}
              data-testid="context-disclaimer"
            />

            <Collapse
              items={[
                {
                  key: 'individual',
                  label: 'Individual Permissions (Context)',
                  children: (
                    <div data-testid="individual-permissions-context">
                      {isLoadingIndividual && <Spin data-testid="individual-loading" />}
                      {!isLoadingIndividual && individualPermissions && individualPermissions.length === 0 && (
                        <Text type="secondary">No individual permissions found.</Text>
                      )}
                      {!isLoadingIndividual && individualPermissions && individualPermissions.length > 0 && (
                        <Table
                          dataSource={individualPermissions}
                          rowKey="id"
                          size="small"
                          pagination={false}
                          data-testid="individual-permissions-table"
                          columns={[
                            { title: 'Permission', dataIndex: 'permissionCode', key: 'permissionCode' },
                            {
                              title: 'Grant',
                              dataIndex: 'grantType',
                              key: 'grantType',
                              render: (gt: string) =>
                                gt === 'ALLOW' ? (
                                  <Tag color="green">ALLOW</Tag>
                                ) : (
                                  <Tag color="red">{gt}</Tag>
                                ),
                            },
                            { title: 'Scope', dataIndex: 'scopeType', key: 'scopeType' },
                            { title: 'Status', dataIndex: 'assignmentStatus', key: 'assignmentStatus' },
                          ]}
                        />
                      )}
                    </div>
                  ),
                },
                {
                  key: 'roles',
                  label: 'Role Assignments (Context)',
                  children: (
                    <div data-testid="role-assignments-context">
                      {isLoadingRoles && <Spin data-testid="roles-loading" />}
                      {!isLoadingRoles && roleAssignments && roleAssignments.length === 0 && (
                        <Text type="secondary">No role assignments found.</Text>
                      )}
                      {!isLoadingRoles && roleAssignments && roleAssignments.length > 0 && (
                        <RoleAssignmentsList assignments={roleAssignments} />
                      )}
                    </div>
                  ),
                },
                {
                  key: 'adminGroups',
                  label: 'Admin Group Assignments (Context)',
                  children: (
                    <div data-testid="admin-group-assignments-context">
                      {isLoadingAdminGroups && <Spin data-testid="admin-groups-loading" />}
                      {!isLoadingAdminGroups && adminGroupAssignments && adminGroupAssignments.length === 0 && (
                        <Text type="secondary">No admin group assignments found.</Text>
                      )}
                      {!isLoadingAdminGroups && adminGroupAssignments && adminGroupAssignments.length > 0 && (
                        <AdminGroupAssignmentsList assignments={adminGroupAssignments} />
                      )}
                    </div>
                  ),
                },
              ]}
            />
          </Card>
        </>
      )}
    </div>
  );
};

const RoleAssignmentsList: React.FC<{ assignments: UserRoleAssignmentDto[] }> = ({ assignments }) => {
  return (
    <List
      dataSource={assignments}
      size="small"
      renderItem={(ra) => (
        <List.Item key={ra.id}>
          <List.Item.Meta
            title={
              <>
                <Text strong>{ra.roleCode}</Text> — {ra.roleName}
                {ra.isActive ? (
                  <Tag color="green" style={{ marginLeft: 8 }}>Active</Tag>
                ) : (
                  <Tag color="red" style={{ marginLeft: 8 }}>Inactive</Tag>
                )}
              </>
            }
            description={`Scope: ${ra.scopeType}${ra.companyId ? ` (Company ${ra.companyId})` : ''}`}
          />
        </List.Item>
      )}
    />
  );
};

const AdminGroupAssignmentsList: React.FC<{ assignments: UserAdminGroupAssignmentDto[] }> = ({ assignments }) => {
  return (
    <List
      dataSource={assignments}
      size="small"
      renderItem={(aga) => (
        <List.Item key={aga.id}>
          <List.Item.Meta
            title={
              <>
                <Text strong>{aga.groupCode}</Text> — {aga.groupName}
                <Tag style={{ marginLeft: 8 }}>{aga.assignmentStatus}</Tag>
              </>
            }
          />
        </List.Item>
      )}
    />
  );
};

export default EffectivePermissionDiagnosticsPage;
