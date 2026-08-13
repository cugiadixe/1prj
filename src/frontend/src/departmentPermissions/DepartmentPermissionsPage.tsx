/**
 * DepartmentPermissionsPage — Phase 1B.1-R.
 *
 * Security admin UI for managing department baseline permissions.
 * Gate: SECURITY_ADMIN_MANAGE GLOBAL.
 * Supports GLOBAL and COMPANY scopes only (ENTITY deferred).
 * COMPANY assignment requires selected current company where relevant.
 * Backend remains authoritative — this is a frontend-only phase.
 */

import React, { useState } from 'react';
import {
  Alert,
  Button,
  Card,
  Descriptions,
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
import { departmentPermissionsApi } from './departmentPermissionsApi';
import type {
  DepartmentDto,
  PermissionDto,
  SetDepartmentPermissionsRequest,
} from './departmentPermissionsApi';
import { getSanitizedErrorMessage } from './errorMessages';
import { isPermissionDenied, PERMISSION_DENIED_MSG } from '../permissionAssignment/errorMessages';

const { Title, Text } = Typography;

// ── Add Permissions Modal ─────────────────────────────────────────────────────

interface AddPermissionsModalProps {
  open: boolean;
  permissions: PermissionDto[];
  isLoading: boolean;
  errorMessage: string | null;
  onSubmit: (selectedCodes: string[]) => void;
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
      onSubmit(selectedPermissions);
    }
  };

  const handleCancel = () => {
    setSelectedPermissions([]);
    onCancel();
  };

  const activePermissions = permissions.filter(p => p.isActive && p.scope !== 'ENTITY');

  return (
    <Modal
      open={open}
      title="Thêm quyền vào chuẩn phòng ban"
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

// ── Main Department Permissions Page ─────────────────────────────────────────────────

const DepartmentPermissionsPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();

  const [selectedDepartmentId, setSelectedDepartmentId] = useState<number | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [showPermissionsModal, setShowPermissionsModal] = useState(false);
  const [permissionsFormError, setPermissionsFormError] = useState<string | null>(null);

  // Queries
  const {
    data: departments,
    isLoading: isLoadingDepartments,
    isError: isDepartmentsError,
    error: departmentsError,
  } = useQuery({
    queryKey: ['departments', currentCompanyId],
    queryFn: () => departmentPermissionsApi.getDepartments(currentCompanyId || undefined),
    retry: false,
  });

  const {
    data: catalog,
  } = useQuery({
    queryKey: ['permission-catalog'],
    queryFn: departmentPermissionsApi.getPermissions,
    retry: false,
  });

  const {
    data: departmentPermissions,
    isLoading: isLoadingDeptPerms,
  } = useQuery({
    queryKey: ['department-permissions', selectedDepartmentId],
    queryFn: () => departmentPermissionsApi.getDepartmentPermissions(selectedDepartmentId!),
    enabled: !!selectedDepartmentId,
    retry: false,
  });

  const selectedDepartment = departments?.find(d => d.id === selectedDepartmentId);

  // Mutations
  const setPermissionsMutation = useMutation({
    mutationFn: (request: SetDepartmentPermissionsRequest) => departmentPermissionsApi.setDepartmentPermissions(selectedDepartmentId!, request),
    onSuccess: () => {
      setShowPermissionsModal(false);
      setPermissionsFormError(null);
      setSuccessMessage('Thêm quyền thành công.');
      void queryClient.invalidateQueries({ queryKey: ['department-permissions', selectedDepartmentId] });
    },
    onError: (err: any) => {
      setPermissionsFormError(getSanitizedErrorMessage(err, 'Không thể cập nhật quyền phòng ban.'));
    },
  });

  const removePermissionMutation = useMutation({
    mutationFn: (code: string) => departmentPermissionsApi.removeDepartmentPermission(selectedDepartmentId!, code),
    onSuccess: () => {
      setSuccessMessage('Gỡ quyền thành công.');
      void queryClient.invalidateQueries({ queryKey: ['department-permissions', selectedDepartmentId] });
    },
    onError: (err: any) => {
      Modal.error({ title: 'Gỡ quyền thất bại', content: getSanitizedErrorMessage(err, 'Không thể gỡ quyền.') });
    },
  });

  // Handlers
  const handleOpenAddPermissions = () => {
    setPermissionsFormError(null);
    setShowPermissionsModal(true);
  };

  const handleAddPermissionsSubmit = (newSelectedCodes: string[]) => {
    // Determine context requirement: if any of the new permissions are COMPANY scoped, require currentCompanyId
    const newPermDetails = catalog?.filter(p => newSelectedCodes.includes(p.permissionCode)) || [];
    const hasCompanyScope = newPermDetails.some(p => p.scope === 'COMPANY');
    if (hasCompanyScope && currentCompanyId === null) {
      setPermissionsFormError('Quyền phạm vi COMPANY yêu cầu phải chọn ngữ cảnh công ty.');
      return;
    }

    // PUT replaces the full baseline set. Append to existing intended permissions.
    const existingCodes = departmentPermissions?.map(p => p.permissionCode) || [];
    // Distinct merge
    const finalCodes = Array.from(new Set([...existingCodes, ...newSelectedCodes]));

    setPermissionsMutation.mutate({ permissionCodes: finalCodes });
  };

  const handleRemovePermission = (code: string) => {
    Modal.confirm({
      title: 'Gỡ quyền',
      content: `Bạn có chắc chắn muốn gỡ ${code} khỏi ${selectedDepartment?.departmentCode}?`,
      okText: 'Gỡ',
      okType: 'danger',
      onOk: () => removePermissionMutation.mutate(code),
    });
  };

  if (isDepartmentsError) {
    return (
      <div data-testid="department-permissions-page">
        <Alert
          type="error"
          message={isPermissionDenied(departmentsError) ? PERMISSION_DENIED_MSG : getSanitizedErrorMessage(departmentsError, 'Không thể tải danh sách phòng ban.')}
          data-testid="departments-error"
        />
      </div>
    );
  }

  return (
    <div data-testid="department-permissions-page">
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={() => navigate(-1)} data-testid="back-button">← Quay lại</Button>
      </Space>

      <Title level={3}>Quyền chuẩn phòng ban</Title>

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

      <Card title="Phòng ban" style={{ marginBottom: 16 }} data-testid="departments-list-card">
        {isLoadingDepartments && <Spin data-testid="departments-loading" />}
        {!isLoadingDepartments && departments && (
          <List
            size="small"
            dataSource={departments}
            data-testid="departments-list"
            renderItem={(d: DepartmentDto) => (
              <List.Item
                key={d.id}
                actions={[
                  <Button key="select" type="link" onClick={() => { setSelectedDepartmentId(d.id); setSuccessMessage(null); }} data-testid={`select-department-${d.id}`}>Chọn</Button>
                ]}
              >
                <List.Item.Meta
                  title={<><Text strong>{d.departmentCode}</Text> {d.isActive ? <Tag color="green">Hoạt động</Tag> : <Tag color="red">Ngừng hoạt động</Tag>}</>}
                  description={d.name}
                />
              </List.Item>
            )}
          />
        )}
      </Card>

      {selectedDepartment && (
        <Card
          title={`Chi tiết phòng ban: ${selectedDepartment.departmentCode}`}
          style={{ marginBottom: 16 }}
          data-testid="department-detail-card"
        >
          <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Tên">{selectedDepartment.name}</Descriptions.Item>
            <Descriptions.Item label="Trạng thái">{selectedDepartment.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}</Descriptions.Item>
          </Descriptions>

          <Card
            type="inner"
            title="Quyền chuẩn"
            extra={<Button size="small" type="primary" onClick={handleOpenAddPermissions} disabled={!selectedDepartment.isActive} data-testid="add-permissions-btn">Thêm quyền</Button>}
          >
            {isLoadingDeptPerms && <Spin data-testid="dept-perms-loading" />}
            {!isLoadingDeptPerms && departmentPermissions && departmentPermissions.length === 0 ? (
              <Text type="secondary">Chưa gán quyền chuẩn nào.</Text>
            ) : (
              !isLoadingDeptPerms && departmentPermissions && (
                <List
                  size="small"
                  dataSource={departmentPermissions}
                  renderItem={(dp) => (
                    <List.Item
                      key={dp.permissionCode}
                      actions={[
                        <Button key="remove" danger type="link" size="small" onClick={() => handleRemovePermission(dp.permissionCode)} disabled={!selectedDepartment.isActive} data-testid={`remove-permission-${dp.permissionCode}`}>Gỡ</Button>
                      ]}
                    >
                      <List.Item.Meta title={dp.permissionCode} />
                    </List.Item>
                  )}
                />
              )
            )}
          </Card>
        </Card>
      )}

      <AddPermissionsModal
        open={showPermissionsModal}
        permissions={catalog || []}
        isLoading={setPermissionsMutation.isPending}
        errorMessage={permissionsFormError}
        onSubmit={handleAddPermissionsSubmit}
        onCancel={() => setShowPermissionsModal(false)}
      />
    </div>
  );
};

export default DepartmentPermissionsPage;
