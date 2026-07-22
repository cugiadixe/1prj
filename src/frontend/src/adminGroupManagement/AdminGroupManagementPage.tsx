/**
 * AdminGroupManagementPage — Phase 1B.1-P2.
 *
 * Security admin UI for managing admin groups and admin group permissions.
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
import { adminGroupManagementApi } from './adminGroupManagementApi';
import type {
  AdminGroupDto,
  CreateAdminGroupRequest,
  UpdateAdminGroupRequest,
  AddAdminGroupPermissionsRequest,
  PermissionDto,
  DeactivateAdminGroupRequest,
} from './adminGroupManagementApi';
import { ADMIN_GROUP_MANAGEMENT_ERRORS } from './errorMessages';
import { isPermissionDenied, PERMISSION_DENIED_MSG } from '../permissionAssignment/errorMessages';

const { Title, Text } = Typography;
const { Option } = Select;

// ── Admin Group Form Modal ───────────────────────────────────────────────────

interface AdminGroupFormModalProps {
  open: boolean;
  initialValues?: AdminGroupDto | null;
  isLoading: boolean;
  errorMessage: string | null;
  onSubmit: (values: CreateAdminGroupRequest | UpdateAdminGroupRequest) => void;
  onCancel: () => void;
}

const AdminGroupFormModal: React.FC<AdminGroupFormModalProps> = ({
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
        } as UpdateAdminGroupRequest);
      } else {
        onSubmit({
          groupCode: values.groupCode,
          name: values.name,
          description: values.description || null,
          scopeType: values.scopeType,
          companyId: null, // Always created with null, backend might require it depending on scope
        } as CreateAdminGroupRequest);
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
      title={isUpdate ? 'Update Admin Group' : 'Create Admin Group'}
      onOk={handleOk}
      onCancel={onCancel}
      confirmLoading={isLoading}
      destroyOnClose
      data-testid="admin-group-form-modal"
    >
      <Form form={form} layout="vertical">
        {!isUpdate && (
          <Form.Item
            name="groupCode"
            label="Group Code"
            rules={[{ required: true, message: 'Group code is required' }]}
          >
            <Input data-testid="admin-group-code-input" />
          </Form.Item>
        )}
        <Form.Item
          name="name"
          label="Name"
          rules={[{ required: true, message: 'Name is required' }]}
        >
          <Input data-testid="admin-group-name-input" />
        </Form.Item>
        <Form.Item name="description" label="Description">
          <Input.TextArea data-testid="admin-group-description-input" />
        </Form.Item>
        {!isUpdate && (
          <Form.Item
            name="scopeType"
            label="Scope Type"
            rules={[{ required: true, message: 'Scope type is required' }]}
          >
            <Select data-testid="admin-group-scope-input">
              <Option value="GLOBAL">GLOBAL</Option>
              <Option value="COMPANY">COMPANY</Option>
            </Select>
          </Form.Item>
        )}
      </Form>
      {errorMessage && (
        <Alert type="error" message={errorMessage} style={{ marginTop: 8 }} data-testid="admin-group-form-error" />
      )}
    </Modal>
  );
};

// ── Add Permissions Modal ────────────────────────────────────────────────────

interface AddPermissionsModalProps {
  open: boolean;
  permissions: PermissionDto[];
  isLoading: boolean;
  errorMessage: string | null;
  onSubmit: (request: AddAdminGroupPermissionsRequest) => void;
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
      title="Add Permissions to Admin Group"
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

// ── Main Admin Group Management Page ─────────────────────────────────────────

const AdminGroupManagementPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();

  const [selectedAdminGroupId, setSelectedAdminGroupId] = useState<number | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modals state
  const [showAdminGroupModal, setShowAdminGroupModal] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [adminGroupFormError, setAdminGroupFormError] = useState<string | null>(null);

  const [showPermissionsModal, setShowPermissionsModal] = useState(false);
  const [permissionsFormError, setPermissionsFormError] = useState<string | null>(null);

  // Queries
  const {
    data: adminGroups,
    isLoading: isLoadingAdminGroups,
    isError: isAdminGroupsError,
    error: adminGroupsError,
  } = useQuery({
    queryKey: ['adminGroups'],
    queryFn: adminGroupManagementApi.getAdminGroups,
    retry: false,
  });

  const { data: catalog } = useQuery({
    queryKey: ['permissionsCatalog'],
    queryFn: adminGroupManagementApi.getPermissions,
  });

  const selectedAdminGroup = adminGroups?.find(g => g.id === selectedAdminGroupId);

  // Mutations
  const createAdminGroupMutation = useMutation({
    mutationFn: adminGroupManagementApi.createAdminGroup,
    onSuccess: (newGroup) => {
      setShowAdminGroupModal(false);
      setAdminGroupFormError(null);
      setSuccessMessage(`Admin Group ${newGroup.groupCode} created successfully.`);
      void queryClient.invalidateQueries({ queryKey: ['adminGroups'] });
    },
    onError: (err: any) => {
      setAdminGroupFormError(err?.response?.data?.detail || ADMIN_GROUP_MANAGEMENT_ERRORS.CREATE_ADMIN_GROUP_FAILED);
    },
  });

  const updateAdminGroupMutation = useMutation({
    mutationFn: (request: UpdateAdminGroupRequest) => adminGroupManagementApi.updateAdminGroup(selectedAdminGroupId!, request),
    onSuccess: (updatedGroup) => {
      setShowAdminGroupModal(false);
      setAdminGroupFormError(null);
      setSuccessMessage(`Admin Group ${updatedGroup.groupCode} updated successfully.`);
      void queryClient.invalidateQueries({ queryKey: ['adminGroups'] });
    },
    onError: (err: any) => {
      setAdminGroupFormError(err?.response?.data?.detail || ADMIN_GROUP_MANAGEMENT_ERRORS.UPDATE_ADMIN_GROUP_FAILED);
    },
  });

  const deactivateAdminGroupMutation = useMutation({
    mutationFn: (request: DeactivateAdminGroupRequest) => adminGroupManagementApi.deactivateAdminGroup(selectedAdminGroupId!, request),
    onSuccess: () => {
      setSuccessMessage('Admin Group deactivated successfully.');
      void queryClient.invalidateQueries({ queryKey: ['adminGroups'] });
      setSelectedAdminGroupId(null);
    },
    onError: (err: any) => {
      Modal.error({ title: 'Deactivate Failed', content: err?.response?.data?.detail || ADMIN_GROUP_MANAGEMENT_ERRORS.DEACTIVATE_ADMIN_GROUP_FAILED });
    },
  });

  const addPermissionsMutation = useMutation({
    mutationFn: (request: AddAdminGroupPermissionsRequest) => adminGroupManagementApi.addAdminGroupPermissions(selectedAdminGroupId!, request),
    onSuccess: () => {
      setShowPermissionsModal(false);
      setPermissionsFormError(null);
      setSuccessMessage('Permissions added successfully.');
      void queryClient.invalidateQueries({ queryKey: ['adminGroups'] });
    },
    onError: (err: any) => {
      setPermissionsFormError(err?.response?.data?.detail || ADMIN_GROUP_MANAGEMENT_ERRORS.ADD_PERMISSION_FAILED);
    },
  });

  const removePermissionMutation = useMutation({
    mutationFn: (code: string) => adminGroupManagementApi.removeAdminGroupPermission(selectedAdminGroupId!, code),
    onSuccess: () => {
      setSuccessMessage('Permission removed successfully.');
      void queryClient.invalidateQueries({ queryKey: ['adminGroups'] });
    },
    onError: (err: any) => {
      Modal.error({ title: 'Remove Failed', content: err?.response?.data?.detail || ADMIN_GROUP_MANAGEMENT_ERRORS.REMOVE_PERMISSION_FAILED });
    },
  });

  // Handlers
  const handleOpenCreateAdminGroup = () => {
    setIsEditMode(false);
    setAdminGroupFormError(null);
    setShowAdminGroupModal(true);
  };

  const handleOpenEditAdminGroup = () => {
    setIsEditMode(true);
    setAdminGroupFormError(null);
    setShowAdminGroupModal(true);
  };

  const handleAdminGroupSubmit = (values: CreateAdminGroupRequest | UpdateAdminGroupRequest) => {
    if (isEditMode) {
      updateAdminGroupMutation.mutate(values as UpdateAdminGroupRequest);
    } else {
      createAdminGroupMutation.mutate(values as CreateAdminGroupRequest);
    }
  };

  const handleDeactivateAdminGroup = () => {
    if (!selectedAdminGroup) return;
    Modal.confirm({
      title: 'Deactivate Admin Group',
      content: `Are you sure you want to deactivate the admin group ${selectedAdminGroup.groupCode}?`,
      okText: 'Deactivate',
      okType: 'danger',
      onOk: () => deactivateAdminGroupMutation.mutate({ rowVersion: selectedAdminGroup.rowVersion }),
    });
  };

  const handleOpenAddPermissions = () => {
    if (selectedAdminGroup?.scopeType === 'COMPANY' && currentCompanyId === null) {
      Modal.error({ title: 'Company Required', content: ADMIN_GROUP_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED });
      return;
    }
    setPermissionsFormError(null);
    setShowPermissionsModal(true);
  };

  const handleRemovePermission = (code: string) => {
    if (selectedAdminGroup?.scopeType === 'COMPANY' && currentCompanyId === null) {
      Modal.error({ title: 'Company Required', content: ADMIN_GROUP_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED });
      return;
    }
    Modal.confirm({
      title: 'Remove Permission',
      content: `Are you sure you want to remove ${code} from ${selectedAdminGroup?.groupCode}?`,
      okText: 'Remove',
      okType: 'danger',
      onOk: () => removePermissionMutation.mutate(code),
    });
  };

  if (isAdminGroupsError) {
    return (
      <div data-testid="admin-group-management-page">
        <Alert
          type="error"
          message={isPermissionDenied(adminGroupsError) ? PERMISSION_DENIED_MSG : ADMIN_GROUP_MANAGEMENT_ERRORS.FETCH_ADMIN_GROUPS_FAILED}
          data-testid="admin-groups-error"
        />
      </div>
    );
  }

  return (
    <div data-testid="admin-group-management-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">← Back</Button>
      </Space>

      <Title level={3}>Admin Group Management</Title>

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

      <Card title="Admin Groups" style={{ marginBottom: 16 }} extra={<Button type="primary" onClick={handleOpenCreateAdminGroup} data-testid="create-admin-group-btn">Create Admin Group</Button>} data-testid="admin-groups-list-card">
        {isLoadingAdminGroups && <Spin data-testid="admin-groups-loading" />}
        {!isLoadingAdminGroups && adminGroups && (
          <List
            size="small"
            dataSource={adminGroups}
            data-testid="admin-groups-list"
            renderItem={(g) => (
              <List.Item
                key={g.id}
                actions={[
                  <Button key="select" type="link" onClick={() => { setSelectedAdminGroupId(g.id); setSuccessMessage(null); }} data-testid={`select-admin-group-${g.id}`}>Select</Button>
                ]}
              >
                <List.Item.Meta
                  title={<><Text strong>{g.groupCode}</Text> {g.isActive ? <Tag color="green">Active</Tag> : <Tag color="red">Inactive</Tag>}</>}
                  description={`${g.name} (${g.scopeType})`}
                />
              </List.Item>
            )}
          />
        )}
      </Card>

      {selectedAdminGroup && (
        <Card
          title={`Admin Group Details: ${selectedAdminGroup.groupCode}`}
          style={{ marginBottom: 16 }}
          data-testid="admin-group-detail-card"
          extra={
            <Space>
              <Button onClick={handleOpenEditAdminGroup} data-testid="edit-admin-group-btn">Edit</Button>
              <Button danger onClick={handleDeactivateAdminGroup} disabled={!selectedAdminGroup.isActive} data-testid="deactivate-admin-group-btn">Deactivate</Button>
            </Space>
          }
        >
          <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Name">{selectedAdminGroup.name}</Descriptions.Item>
            <Descriptions.Item label="Description">{selectedAdminGroup.description || '—'}</Descriptions.Item>
            <Descriptions.Item label="Scope">{selectedAdminGroup.scopeType}</Descriptions.Item>
            <Descriptions.Item label="Status">{selectedAdminGroup.isActive ? 'Active' : 'Inactive'}</Descriptions.Item>
          </Descriptions>

          <Card
            type="inner"
            title="Permissions"
            extra={<Button size="small" type="primary" onClick={handleOpenAddPermissions} disabled={!selectedAdminGroup.isActive} data-testid="add-permissions-btn">Add Permissions</Button>}
          >
            {selectedAdminGroup.scopeType === 'COMPANY' && currentCompanyId === null && (
              <Alert type="warning" message={ADMIN_GROUP_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED} style={{ marginBottom: 16 }} data-testid="company-required-warning" />
            )}
            {selectedAdminGroup.permissionCodes.length === 0 ? (
              <Text type="secondary">No permissions assigned.</Text>
            ) : (
              <List
                size="small"
                dataSource={selectedAdminGroup.permissionCodes}
                renderItem={(code) => (
                  <List.Item
                    key={code}
                    actions={[
                      <Button key="remove" danger type="link" size="small" onClick={() => handleRemovePermission(code)} disabled={!selectedAdminGroup.isActive} data-testid={`remove-permission-${code}`}>Remove</Button>
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

      <AdminGroupFormModal
        open={showAdminGroupModal}
        initialValues={isEditMode ? selectedAdminGroup : null}
        isLoading={isEditMode ? updateAdminGroupMutation.isPending : createAdminGroupMutation.isPending}
        errorMessage={adminGroupFormError}
        onSubmit={handleAdminGroupSubmit}
        onCancel={() => setShowAdminGroupModal(false)}
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

export default AdminGroupManagementPage;
