import React, { useState, useMemo } from 'react';
import {
  Alert,
  Button,
  DatePicker,
  Form,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import dayjs from 'dayjs';

import { useCompany } from '../auth/CompanyProvider';
import { roleManagementApi } from '../roleManagement/roleManagementApi';
import { userRoleAssignmentsApi } from './userRoleAssignmentsApi';
import type {
  CreateUserRoleAssignmentRequest,
  DeactivateAssignmentRequest,
} from './userRoleAssignmentsApi';
import {
  getErrorMessage,
  isPermissionDenied,
  isNotFound,
  PERMISSION_DENIED,
  NOT_FOUND,
} from './errorMessages';
import type { UserRoleAssignmentDto } from './userRoleAssignmentsApi';

const { Title, Text } = Typography;

const UserRoleAssignmentsPage: React.FC = () => {
  const { userId } = useParams<{ userId: string }>();
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();

  const userIdNum = userId ? parseInt(userId, 10) : NaN;

  const [isAssignModalVisible, setIsAssignModalVisible] = useState(false);
  const [deactivatingAssignment, setDeactivatingAssignment] = useState<UserRoleAssignmentDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const [form] = Form.useForm();

  // Queries
  const {
    data: assignments,
    isLoading: isLoadingAssignments,
    isError: isAssignmentsError,
    error: assignmentsError,
  } = useQuery({
    queryKey: ['user-role-assignments', userIdNum],
    queryFn: () => userRoleAssignmentsApi.getUserRoleAssignments(userIdNum),
    enabled: !isNaN(userIdNum),
  });

  const { data: roles = [], isLoading: isLoadingRoles } = useQuery({
    queryKey: ['roles'],
    queryFn: () => roleManagementApi.getRoles(),
  });

  // Mutations
  const assignMutation = useMutation({
    mutationFn: async (values: any) => {
      if (isNaN(userIdNum)) return;
      const roleId = values.roleId as number;
      const role = roles.find(r => r.id === roleId);

      // Enforce company context for COMPANY scoped roles
      let companyIdToPass: number | undefined = undefined;
      if (role?.scopeType === 'COMPANY') {
        if (!currentCompanyId) {
          throw new Error('COMPANY_CONTEXT_REQUIRED');
        }
        companyIdToPass = currentCompanyId;
      }

      const request: CreateUserRoleAssignmentRequest = {
        roleId,
        effectiveFrom: values.effectiveFrom.toISOString(),
        effectiveTo: values.effectiveTo ? values.effectiveTo.toISOString() : null,
      };

      await userRoleAssignmentsApi.assignRoleToUser(userIdNum, request, companyIdToPass);
    },
    onSuccess: () => {
      setIsAssignModalVisible(false);
      form.resetFields();
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ['user-role-assignments', userIdNum] });
    },
    onError: (err: unknown) => {
      const errMessage = getErrorMessage(err);
      if (err instanceof Error && err.message === 'COMPANY_CONTEXT_REQUIRED') {
        setActionError('Phải chọn một công ty cụ thể để phân công vai trò có phạm vi COMPANY.');
      } else {
        setActionError(errMessage);
      }
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: async (assignment: UserRoleAssignmentDto) => {
      if (isNaN(userIdNum)) return;
      
      let companyIdToPass: number | undefined = undefined;
      if (assignment.scopeType === 'COMPANY') {
        if (!currentCompanyId) {
            throw new Error('COMPANY_CONTEXT_REQUIRED');
        }
        companyIdToPass = currentCompanyId;
      }

      const request: DeactivateAssignmentRequest = { rowVersion: assignment.rowVersion };
      await userRoleAssignmentsApi.deactivateUserRoleAssignment(userIdNum, assignment.id, request, companyIdToPass);
    },
    onSuccess: () => {
      setDeactivatingAssignment(null);
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ['user-role-assignments', userIdNum] });
    },
    onError: (err: unknown) => {
      const errMessage = getErrorMessage(err);
      if (err instanceof Error && err.message === 'COMPANY_CONTEXT_REQUIRED') {
        setActionError('Phải chọn một công ty cụ thể để hủy phân công có phạm vi COMPANY này.');
      } else {
        setActionError(errMessage);
      }
    },
  });

  // Derived state
  const activeRoles = useMemo(() => roles.filter(r => r.isActive), [roles]);

  // Handlers
  const handleAssignClick = () => {
    setActionError(null);
    form.resetFields();
    setIsAssignModalVisible(true);
  };

  const handleAssignCancel = () => {
    setIsAssignModalVisible(false);
    setActionError(null);
    assignMutation.reset();
  };

  const handleAssignSubmit = () => {
    form.validateFields().then(values => {
      setActionError(null);
      assignMutation.mutate(values);
    });
  };

  const handleDeactivateClick = (assignment: UserRoleAssignmentDto) => {
    setActionError(null);
    setDeactivatingAssignment(assignment);
  };

  const handleDeactivateCancel = () => {
    setDeactivatingAssignment(null);
    setActionError(null);
    deactivateMutation.reset();
  };

  const handleDeactivateConfirm = () => {
    if (deactivatingAssignment) {
      setActionError(null);
      deactivateMutation.mutate(deactivatingAssignment);
    }
  };

  // Render logic
  if (isNaN(userIdNum)) {
    return <Alert type="error" message="Mã người dùng không hợp lệ." data-testid="invalid-user-id" />;
  }

  if (isAssignmentsError) {
    if (isPermissionDenied(assignmentsError)) {
      return <Alert type="warning" message={PERMISSION_DENIED} data-testid="assignments-permission-denied" />;
    }
    if (isNotFound(assignmentsError)) {
      return <Alert type="error" message={NOT_FOUND} data-testid="assignments-not-found" />;
    }
    return <Alert type="error" message={getErrorMessage(assignmentsError)} data-testid="assignments-error" />;
  }

  const columns = [
    {
      title: 'Vai trò',
      key: 'role',
      render: (record: UserRoleAssignmentDto) => (
        <Space direction="vertical" size={0}>
          <Text strong data-testid={`assignment-role-name-${record.id}`}>{record.roleName}</Text>
          <Text type="secondary" data-testid={`assignment-role-code-${record.id}`}>{record.roleCode}</Text>
        </Space>
      ),
    },
    {
      title: 'Phạm vi',
      dataIndex: 'scopeType',
      key: 'scopeType',
      render: (scopeType: string) => <Tag data-testid={`assignment-scope-${scopeType}`}>{scopeType}</Tag>,
    },
    {
      title: 'Hiệu lực từ',
      dataIndex: 'effectiveFrom',
      key: 'effectiveFrom',
      render: (val: string) => new Date(val).toLocaleString('vi-VN'),
    },
    {
      title: 'Hiệu lực đến',
      dataIndex: 'effectiveTo',
      key: 'effectiveTo',
      render: (val: string | null) => (val ? new Date(val).toLocaleString('vi-VN') : '—'),
    },
    {
      title: 'Trạng thái',
      key: 'status',
      render: (record: UserRoleAssignmentDto) => {
        const isPastEffectiveTo = record.effectiveTo ? new Date(record.effectiveTo) < new Date() : false;
        const effectivelyActive = record.isActive && !isPastEffectiveTo;
        return (
          <Tag color={effectivelyActive ? 'green' : 'default'} data-testid={`assignment-status-${record.id}`}>
            {effectivelyActive ? 'HOẠT ĐỘNG' : 'NGỪNG HOẠT ĐỘNG'}
          </Tag>
        );
      },
    },
    {
      title: 'Hành động',
      key: 'action',
      render: (record: UserRoleAssignmentDto) => (
        record.isActive && (
          <Button
            type="link"
            danger
            onClick={() => handleDeactivateClick(record)}
            data-testid={`deactivate-assignment-button-${record.id}`}
          >
            Vô hiệu hóa
          </Button>
        )
      ),
    },
  ];

  return (
    <div data-testid="user-role-assignments-page">
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <div>
            <Title level={3}>Phân vai trò người dùng</Title>
            <Text type="secondary" data-testid="user-id-display">Mã người dùng: {userIdNum}</Text>
          </div>
          <Space>
            <Button
              type="primary"
              onClick={handleAssignClick}
              data-testid="assign-role-button"
            >
              Phân vai trò
            </Button>
          </Space>
        </div>

        <Table
          dataSource={assignments}
          columns={columns}
          rowKey="id"
          loading={isLoadingAssignments}
          pagination={false}
          data-testid="assignments-table"
        />
      </Space>

      {/* Assign Role Modal */}
      <Modal
        open={isAssignModalVisible}
        title="Phân vai trò"
        onCancel={handleAssignCancel}
        onOk={handleAssignSubmit}
        confirmLoading={assignMutation.isPending}
        destroyOnClose
        data-testid="assign-role-modal"
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="roleId"
            label="Vai trò"
            rules={[{ required: true, message: 'Vui lòng chọn một vai trò.' }]}
          >
            <Select
              loading={isLoadingRoles}
              placeholder="Chọn một vai trò"
              data-testid="assign-role-select"
            >
              {activeRoles.map(role => (
                <Select.Option key={role.id} value={role.id} data-testid={`role-option-${role.id}`}>
                  {role.name} ({role.scopeType})
                </Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="effectiveFrom"
            label="Hiệu lực từ"
            rules={[{ required: true, message: 'Ngày hiệu lực từ là bắt buộc.' }]}
            initialValue={dayjs()}
          >
            <DatePicker showTime style={{ width: '100%' }} data-testid="effective-from-picker" />
          </Form.Item>

          <Form.Item
            name="effectiveTo"
            label="Hiệu lực đến"
          >
            <DatePicker showTime style={{ width: '100%' }} data-testid="effective-to-picker" />
          </Form.Item>
        </Form>
        {actionError && (
          <Alert type="error" message={actionError} style={{ marginTop: 16 }} data-testid="assign-error" />
        )}
      </Modal>

      {/* Deactivate Assignment Modal */}
      <Modal
        open={!!deactivatingAssignment}
        title="Vô hiệu hóa phân vai trò"
        onCancel={handleDeactivateCancel}
        onOk={handleDeactivateConfirm}
        confirmLoading={deactivateMutation.isPending}
        okButtonProps={{ danger: true }}
        okText="Vô hiệu hóa"
        destroyOnClose
        data-testid="deactivate-assignment-modal"
      >
        <p>Bạn có chắc chắn muốn vô hiệu hóa phân vai trò cho <strong>{deactivatingAssignment?.roleName}</strong>?</p>
        <Alert type="warning" message="Hành động này sẽ thu hồi ngay lập tức vai trò khỏi người dùng." style={{ marginBottom: 16 }} />
        {actionError && (
          <Alert type="error" message={actionError} data-testid="deactivate-error" />
        )}
      </Modal>
    </div>
  );
};

export default UserRoleAssignmentsPage;
