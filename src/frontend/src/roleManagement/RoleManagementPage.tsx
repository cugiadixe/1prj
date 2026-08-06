/**
 * RoleManagementPage — Phase 1B.1-P1.
 *
 * Security admin UI for managing roles and role permissions.
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
import { useNavigate } from 'react-router-dom';
import { useCompany } from '../auth/CompanyProvider';
import { roleManagementApi } from './roleManagementApi';
import type {
  RoleDto,
  CreateRoleRequest,
  UpdateRoleRequest,
  AddRolePermissionsRequest,
  PermissionDto,
  DeactivateRoleRequest,
} from './roleManagementApi';
import { ROLE_MANAGEMENT_ERRORS } from './errorMessages';
import { isPermissionDenied, PERMISSION_DENIED_MSG } from '../permissionAssignment/errorMessages';

const { Title, Text } = Typography;
const { Option } = Select;

// ── Role Form Modal ──────────────────────────────────────────────────────────

interface RoleFormModalProps {
  open: boolean;
  initialValues?: RoleDto | null;
  isLoading: boolean;
  errorMessage: string | null;
  onSubmit: (values: CreateRoleRequest | UpdateRoleRequest) => void;
  onCancel: () => void;
}

const RoleFormModal: React.FC<RoleFormModalProps> = ({
  open,
  initialValues,
  isLoading,
  errorMessage,
  onSubmit,
  onCancel,
}) => {
  const [form] = Form.useForm();
  const isUpdate = !!initialValues;

  const handleOk = () => {
    form.validateFields().then(values => {
      if (isUpdate && initialValues) {
        onSubmit({
          name: values.name,
          description: values.description || null,
          rowVersion: initialValues.rowVersion,
        } as UpdateRoleRequest);
      } else {
        onSubmit({
          roleCode: values.roleCode,
          name: values.name,
          description: values.description || null,
          scopeType: values.scopeType,
          companyId: null, // Always created with null, backend might require it depending on scope
        } as CreateRoleRequest);
      }
    }).catch(() => {});
  };

  // Reset form when opened or initialValues change
  React.useEffect(() => {
    if (open) {
      if (initialValues) {
        form.setFieldsValue(initialValues);
      } else {
        form.resetFields();
        form.setFieldsValue({ scopeType: 'GLOBAL' });
      }
    }
  }, [open, initialValues, form]);

  return (
    <Modal
      open={open}
      title={isUpdate ? 'Update Role' : 'Create Role'}
      onOk={handleOk}
      onCancel={onCancel}
      confirmLoading={isLoading}
      destroyOnClose
      data-testid="role-form-modal"
    >
      <Form form={form} layout="vertical">
        {!isUpdate && (
          <Form.Item
            name="roleCode"
            label="Role Code"
            rules={[{ required: true, message: 'Role code is required' }]}
          >
            <Input data-testid="role-code-input" />
          </Form.Item>
        )}
        <Form.Item
          name="name"
          label="Name"
          rules={[{ required: true, message: 'Name is required' }]}
        >
          <Input data-testid="role-name-input" />
        </Form.Item>
        <Form.Item name="description" label="Description">
          <Input.TextArea data-testid="role-description-input" />
        </Form.Item>
        {!isUpdate && (
          <Form.Item
            name="scopeType"
            label="Scope Type"
            rules={[{ required: true, message: 'Scope type is required' }]}
          >
            <Select data-testid="role-scope-input">
              <Option value="GLOBAL">GLOBAL</Option>
              <Option value="COMPANY">COMPANY</Option>
            </Select>
          </Form.Item>
        )}
      </Form>
      {errorMessage && (
        <Alert type="error" message={errorMessage} style={{ marginTop: 8 }} data-testid="role-form-error" />
      )}
    </Modal>
  );
};

// ── Add Permissions Modal ─────────────────────────────────────────────────────

interface AddPermissionsModalProps {
  open: boolean;
  permissions: PermissionDto[];
  isLoading: boolean;
  errorMessage: string | null;
  onSubmit: (request: AddRolePermissionsRequest) => void;
  onCancel: () => void;
}

const AddPermissionsModal: React.FC<AddPermissionsModalProps> = ({
  open,
  permissions,
  isLoading,
  errorMessage,
  onSubmit,
  onCancel,
}) => {
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);

  const handleOk = () => {
    if (selectedPermissions.length > 0) {
      onSubmit({ permissionCodes: selectedPermissions });
    }
  };

  const handleCancel = () => {
    setSelectedPermissions([]);
    onCancel();
  };

  const activePermissions = permissions.filter(p => p.isActive);

  return (
    <Modal
      open={open}
      title="Add Permissions to Role"
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={isLoading}
      okButtonProps={{ disabled: selectedPermissions.length === 0 }}
      destroyOnClose
      data-testid="add-permissions-modal"
    >
      <Select
        mode="multiple"
        style={{ width: '100%' }}
        placeholder="Select permissions"
        value={selectedPermissions}
        onChange={setSelectedPermissions}
        data-testid="permissions-select"
        optionLabelProp="label"
        options={activePermissions.map(p => ({
          label: p.permissionCode,
          value: p.permissionCode,
          desc: p.description
        }))}
        optionRender={(option) => (
          <div>
            <strong>{option.data.value}</strong>
            {option.data.desc && <div style={{ fontSize: '0.8em', color: '#666' }}>{option.data.desc}</div>}
          </div>
        )}
      />
      {errorMessage && (
        <Alert type="error" message={errorMessage} style={{ marginTop: 8 }} data-testid="add-permissions-error" />
      )}
    </Modal>
  );
};

// ── Main Role Management Page ─────────────────────────────────────────────────

const RoleManagementPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();

  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modals state
  const [showRoleModal, setShowRoleModal] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [roleFormError, setRoleFormError] = useState<string | null>(null);

  const [showPermissionsModal, setShowPermissionsModal] = useState(false);
  const [permissionsFormError, setPermissionsFormError] = useState<string | null>(null);

  // Queries
  const {
    data: roles,
    isLoading: isLoadingRoles,
    isError: isRolesError,
    error: rolesError,
  } = useQuery({
    queryKey: ['roles'],
    queryFn: roleManagementApi.getRoles,
    retry: false,
  });

  const {
    data: catalog,
  } = useQuery({
    queryKey: ['permission-catalog'],
    queryFn: roleManagementApi.getPermissions,
    retry: false,
  });

  const selectedRole = roles?.find(r => r.id === selectedRoleId);

  // Mutations
  const createRoleMutation = useMutation({
    mutationFn: roleManagementApi.createRole,
    onSuccess: (newRole) => {
      setShowRoleModal(false);
      setRoleFormError(null);
      setSuccessMessage('Role created successfully.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      setSelectedRoleId(newRole.id);
    },
    onError: (err: any) => {
      setRoleFormError(err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.CREATE_ROLE_FAILED);
    },
  });

  const updateRoleMutation = useMutation({
    mutationFn: (request: UpdateRoleRequest) => roleManagementApi.updateRole(selectedRoleId!, request),
    onSuccess: () => {
      setShowRoleModal(false);
      setRoleFormError(null);
      setSuccessMessage('Role updated successfully.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
    },
    onError: (err: any) => {
      setRoleFormError(err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.UPDATE_ROLE_FAILED);
    },
  });

  const deactivateRoleMutation = useMutation({
    mutationFn: (request: DeactivateRoleRequest) => roleManagementApi.deactivateRole(selectedRoleId!, request),
    onSuccess: () => {
      setSuccessMessage('Role deactivated successfully.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      setSelectedRoleId(null);
    },
    onError: (err: any) => {
      Modal.error({ title: 'Deactivate Failed', content: err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.DEACTIVATE_ROLE_FAILED });
    },
  });

  const addPermissionsMutation = useMutation({
    mutationFn: (request: AddRolePermissionsRequest) => roleManagementApi.addRolePermissions(selectedRoleId!, request),
    onSuccess: () => {
      setShowPermissionsModal(false);
      setPermissionsFormError(null);
      setSuccessMessage('Permissions added successfully.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
    },
    onError: (err: any) => {
      setPermissionsFormError(err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.ADD_PERMISSION_FAILED);
    },
  });

  const removePermissionMutation = useMutation({
    mutationFn: (code: string) => roleManagementApi.removeRolePermission(selectedRoleId!, code),
    onSuccess: () => {
      setSuccessMessage('Permission removed successfully.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
    },
    onError: (err: any) => {
      Modal.error({ title: 'Remove Failed', content: err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.REMOVE_PERMISSION_FAILED });
    },
  });

  // Handlers
  const handleOpenCreateRole = () => {
    setIsEditMode(false);
    setRoleFormError(null);
    setShowRoleModal(true);
  };

  const handleOpenEditRole = () => {
    setIsEditMode(true);
    setRoleFormError(null);
    setShowRoleModal(true);
  };

  const handleRoleSubmit = (values: CreateRoleRequest | UpdateRoleRequest) => {
    if (isEditMode) {
      updateRoleMutation.mutate(values as UpdateRoleRequest);
    } else {
      createRoleMutation.mutate(values as CreateRoleRequest);
    }
  };

  const handleDeactivateRole = () => {
    if (!selectedRole) return;
    Modal.confirm({
      title: 'Deactivate Role',
      content: `Are you sure you want to deactivate the role ${selectedRole.roleCode}?`,
      okText: 'Deactivate',
      okType: 'danger',
      onOk: () => deactivateRoleMutation.mutate({ rowVersion: selectedRole.rowVersion }),
    });
  };

  const handleOpenAddPermissions = () => {
    if (selectedRole?.scopeType === 'COMPANY' && currentCompanyId === null) {
      Modal.error({ title: 'Company Required', content: ROLE_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED });
      return;
    }
    setPermissionsFormError(null);
    setShowPermissionsModal(true);
  };

  const handleRemovePermission = (code: string) => {
    if (selectedRole?.scopeType === 'COMPANY' && currentCompanyId === null) {
      Modal.error({ title: 'Company Required', content: ROLE_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED });
      return;
    }
    Modal.confirm({
      title: 'Remove Permission',
      content: `Are you sure you want to remove ${code} from ${selectedRole?.roleCode}?`,
      okText: 'Remove',
      okType: 'danger',
      onOk: () => removePermissionMutation.mutate(code),
    });
  };

  if (isRolesError) {
    return (
      <div data-testid="role-management-page">
        <Alert
          type="error"
          message={isPermissionDenied(rolesError) ? PERMISSION_DENIED_MSG : ROLE_MANAGEMENT_ERRORS.FETCH_ROLES_FAILED}
          data-testid="roles-error"
        />
      </div>
    );
  }

  return (
    <div data-testid="role-management-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">← Back</Button>
      </Space>

      <Title level={3}>Role Management</Title>

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

      <Card title="Roles" style={{ marginBottom: 16 }} extra={<Button type="primary" onClick={handleOpenCreateRole} data-testid="create-role-btn">Create Role</Button>} data-testid="roles-list-card">
        {isLoadingRoles && <Spin data-testid="roles-loading" />}
        {!isLoadingRoles && roles && (
          <List
            size="small"
            dataSource={roles}
            data-testid="roles-list"
            renderItem={(r) => (
              <List.Item
                key={r.id}
                actions={[
                  <Button key="select" type="link" onClick={() => { setSelectedRoleId(r.id); setSuccessMessage(null); }} data-testid={`select-role-${r.id}`}>Select</Button>
                ]}
              >
                <List.Item.Meta
                  title={<><Text strong>{r.roleCode}</Text> {r.isActive ? <Tag color="green">Active</Tag> : <Tag color="red">Inactive</Tag>}</>}
                  description={`${r.name} (${r.scopeType})`}
                />
              </List.Item>
            )}
          />
        )}
      </Card>

      {selectedRole && (
        <Card
          title={`Role Details: ${selectedRole.roleCode}`}
          style={{ marginBottom: 16 }}
          data-testid="role-detail-card"
          extra={
            <Space>
              <Button onClick={handleOpenEditRole} data-testid="edit-role-btn">Edit</Button>
              <Button danger onClick={handleDeactivateRole} disabled={!selectedRole.isActive} data-testid="deactivate-role-btn">Deactivate</Button>
            </Space>
          }
        >
          <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Name">{selectedRole.name}</Descriptions.Item>
            <Descriptions.Item label="Description">{selectedRole.description || '—'}</Descriptions.Item>
            <Descriptions.Item label="Scope">{selectedRole.scopeType}</Descriptions.Item>
            <Descriptions.Item label="Status">{selectedRole.isActive ? 'Active' : 'Inactive'}</Descriptions.Item>
          </Descriptions>

          <Card
            type="inner"
            title="Permissions"
            extra={<Button size="small" type="primary" onClick={handleOpenAddPermissions} disabled={!selectedRole.isActive} data-testid="add-permissions-btn">Add Permissions</Button>}
          >
            {selectedRole.scopeType === 'COMPANY' && currentCompanyId === null && (
              <Alert type="warning" message={ROLE_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED} style={{ marginBottom: 16 }} data-testid="company-required-warning" />
            )}
            {selectedRole.permissionCodes.length === 0 ? (
              <Text type="secondary">No permissions assigned.</Text>
            ) : (
              <List
                size="small"
                dataSource={selectedRole.permissionCodes}
                renderItem={(code) => (
                  <List.Item
                    key={code}
                    actions={[
                      <Button key="remove" danger type="link" size="small" onClick={() => handleRemovePermission(code)} disabled={!selectedRole.isActive} data-testid={`remove-permission-${code}`}>Remove</Button>
                    ]}
                  >
                    <List.Item.Meta title={code} />
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Card>
      )}

      <RoleFormModal
        open={showRoleModal}
        initialValues={isEditMode ? selectedRole : null}
        isLoading={isEditMode ? updateRoleMutation.isPending : createRoleMutation.isPending}
        errorMessage={roleFormError}
        onSubmit={handleRoleSubmit}
        onCancel={() => setShowRoleModal(false)}
      />

      <AddPermissionsModal
        open={showPermissionsModal}
        permissions={catalog || []}
        isLoading={addPermissionsMutation.isPending}
        errorMessage={permissionsFormError}
        onSubmit={(request) => addPermissionsMutation.mutate(request)}
        onCancel={() => setShowPermissionsModal(false)}
      />
    </div>
  );
};

export default RoleManagementPage;
