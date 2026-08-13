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
      title={isUpdate ? 'Cập nhật vai trò' : 'Tạo vai trò'}
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
            label="Mã vai trò"
            rules={[{ required: true, message: 'Mã vai trò là bắt buộc' }]}
          >
            <Input data-testid="role-code-input" />
          </Form.Item>
        )}
        <Form.Item
          name="name"
          label="Tên"
          rules={[{ required: true, message: 'Tên là bắt buộc' }]}
        >
          <Input data-testid="role-name-input" />
        </Form.Item>
        <Form.Item name="description" label="Mô tả">
          <Input.TextArea data-testid="role-description-input" />
        </Form.Item>
        {!isUpdate && (
          <Form.Item
            name="scopeType"
            label="Loại phạm vi"
            rules={[{ required: true, message: 'Loại phạm vi là bắt buộc' }]}
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
      title="Thêm quyền vào vai trò"
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
        placeholder="Chọn quyền"
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
      setSuccessMessage('Tạo vai trò thành công.');
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
      setSuccessMessage('Cập nhật vai trò thành công.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
    },
    onError: (err: any) => {
      setRoleFormError(err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.UPDATE_ROLE_FAILED);
    },
  });

  const deactivateRoleMutation = useMutation({
    mutationFn: (request: DeactivateRoleRequest) => roleManagementApi.deactivateRole(selectedRoleId!, request),
    onSuccess: () => {
      setSuccessMessage('Vô hiệu hóa vai trò thành công.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      setSelectedRoleId(null);
    },
    onError: (err: any) => {
      Modal.error({ title: 'Vô hiệu hóa thất bại', content: err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.DEACTIVATE_ROLE_FAILED });
    },
  });

  const addPermissionsMutation = useMutation({
    mutationFn: (request: AddRolePermissionsRequest) => roleManagementApi.addRolePermissions(selectedRoleId!, request),
    onSuccess: () => {
      setShowPermissionsModal(false);
      setPermissionsFormError(null);
      setSuccessMessage('Thêm quyền thành công.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
    },
    onError: (err: any) => {
      setPermissionsFormError(err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.ADD_PERMISSION_FAILED);
    },
  });

  const removePermissionMutation = useMutation({
    mutationFn: (code: string) => roleManagementApi.removeRolePermission(selectedRoleId!, code),
    onSuccess: () => {
      setSuccessMessage('Gỡ quyền thành công.');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
    },
    onError: (err: any) => {
      Modal.error({ title: 'Gỡ quyền thất bại', content: err?.response?.data?.detail || ROLE_MANAGEMENT_ERRORS.REMOVE_PERMISSION_FAILED });
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
      title: 'Vô hiệu hóa vai trò',
      content: `Bạn có chắc chắn muốn vô hiệu hóa vai trò ${selectedRole.roleCode}?`,
      okText: 'Vô hiệu hóa',
      okType: 'danger',
      onOk: () => deactivateRoleMutation.mutate({ rowVersion: selectedRole.rowVersion }),
    });
  };

  const handleOpenAddPermissions = () => {
    if (selectedRole?.scopeType === 'COMPANY' && currentCompanyId === null) {
      Modal.error({ title: 'Cần chọn công ty', content: ROLE_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED });
      return;
    }
    setPermissionsFormError(null);
    setShowPermissionsModal(true);
  };

  const handleRemovePermission = (code: string) => {
    if (selectedRole?.scopeType === 'COMPANY' && currentCompanyId === null) {
      Modal.error({ title: 'Cần chọn công ty', content: ROLE_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED });
      return;
    }
    Modal.confirm({
      title: 'Gỡ quyền',
      content: `Bạn có chắc chắn muốn gỡ ${code} khỏi ${selectedRole?.roleCode}?`,
      okText: 'Gỡ',
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
        <Button onClick={() => navigate(-1)} data-testid="back-button">← Quay lại</Button>
      </Space>

      <Title level={3}>Quản lý vai trò</Title>

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

      <Card title="Vai trò" style={{ marginBottom: 16 }} extra={<Button type="primary" onClick={handleOpenCreateRole} data-testid="create-role-btn">Tạo vai trò</Button>} data-testid="roles-list-card">
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
                  <Button key="select" type="link" onClick={() => { setSelectedRoleId(r.id); setSuccessMessage(null); }} data-testid={`select-role-${r.id}`}>Chọn</Button>
                ]}
              >
                <List.Item.Meta
                  title={<><Text strong>{r.roleCode}</Text> {r.isActive ? <Tag color="green">Hoạt động</Tag> : <Tag color="red">Ngừng hoạt động</Tag>}</>}
                  description={`${r.name} (${r.scopeType})`}
                />
              </List.Item>
            )}
          />
        )}
      </Card>

      {selectedRole && (
        <Card
          title={`Chi tiết vai trò: ${selectedRole.roleCode}`}
          style={{ marginBottom: 16 }}
          data-testid="role-detail-card"
          extra={
            <Space>
              <Button onClick={handleOpenEditRole} data-testid="edit-role-btn">Sửa</Button>
              <Button danger onClick={handleDeactivateRole} disabled={!selectedRole.isActive} data-testid="deactivate-role-btn">Vô hiệu hóa</Button>
            </Space>
          }
        >
          <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Tên">{selectedRole.name}</Descriptions.Item>
            <Descriptions.Item label="Mô tả">{selectedRole.description || '—'}</Descriptions.Item>
            <Descriptions.Item label="Phạm vi">{selectedRole.scopeType}</Descriptions.Item>
            <Descriptions.Item label="Trạng thái">{selectedRole.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}</Descriptions.Item>
          </Descriptions>

          <Card
            type="inner"
            title="Quyền"
            extra={<Button size="small" type="primary" onClick={handleOpenAddPermissions} disabled={!selectedRole.isActive} data-testid="add-permissions-btn">Thêm quyền</Button>}
          >
            {selectedRole.scopeType === 'COMPANY' && currentCompanyId === null && (
              <Alert type="warning" message={ROLE_MANAGEMENT_ERRORS.COMPANY_CONTEXT_REQUIRED} style={{ marginBottom: 16 }} data-testid="company-required-warning" />
            )}
            {selectedRole.permissionCodes.length === 0 ? (
              <Text type="secondary">Chưa gán quyền nào.</Text>
            ) : (
              <List
                size="small"
                dataSource={selectedRole.permissionCodes}
                renderItem={(code) => (
                  <List.Item
                    key={code}
                    actions={[
                      <Button key="remove" danger type="link" size="small" onClick={() => handleRemovePermission(code)} disabled={!selectedRole.isActive} data-testid={`remove-permission-${code}`}>Gỡ</Button>
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
