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
        setActionError('A specific company must be selected to assign a COMPANY-scoped role.');
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
        setActionError('A specific company must be selected to deactivate this COMPANY-scoped assignment.');
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
    return <Alert type="error" message="Invalid User ID." data-testid="invalid-user-id" />;
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
      title: 'Role',
      key: 'role',
      render: (record: UserRoleAssignmentDto) => (
        <Space direction="vertical" size={0}>
          <Text strong data-testid={`assignment-role-name-${record.id}`}>{record.roleName}</Text>
          <Text type="secondary" data-testid={`assignment-role-code-${record.id}`}>{record.roleCode}</Text>
        </Space>
      ),
    },
    {
      title: 'Scope',
      dataIndex: 'scopeType',
      key: 'scopeType',
      render: (scopeType: string) => <Tag data-testid={`assignment-scope-${scopeType}`}>{scopeType}</Tag>,
    },
    {
      title: 'Effective From',
      dataIndex: 'effectiveFrom',
      key: 'effectiveFrom',
      render: (val: string) => new Date(val).toLocaleString(),
    },
    {
      title: 'Effective To',
      dataIndex: 'effectiveTo',
      key: 'effectiveTo',
      render: (val: string | null) => (val ? new Date(val).toLocaleString() : '—'),
    },
    {
      title: 'Status',
      key: 'status',
      render: (record: UserRoleAssignmentDto) => {
        const isPastEffectiveTo = record.effectiveTo ? new Date(record.effectiveTo) < new Date() : false;
        const effectivelyActive = record.isActive && !isPastEffectiveTo;
        return (
          <Tag color={effectivelyActive ? 'green' : 'default'} data-testid={`assignment-status-${record.id}`}>
            {effectivelyActive ? 'ACTIVE' : 'INACTIVE'}
          </Tag>
        );
      },
    },
    {
      title: 'Action',
      key: 'action',
      render: (record: UserRoleAssignmentDto) => (
        record.isActive && (
          <Button
            type="link"
            danger
            onClick={() => handleDeactivateClick(record)}
            data-testid={`deactivate-assignment-button-${record.id}`}
          >
            Deactivate
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
            <Title level={3}>User Role Assignments</Title>
            <Text type="secondary" data-testid="user-id-display">User ID: {userIdNum}</Text>
          </div>
          <Space>
            <Button
              type="primary"
              onClick={handleAssignClick}
              data-testid="assign-role-button"
            >
              Assign Role
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
        title="Assign Role"
        onCancel={handleAssignCancel}
        onOk={handleAssignSubmit}
        confirmLoading={assignMutation.isPending}
        destroyOnClose
        data-testid="assign-role-modal"
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="roleId"
            label="Role"
            rules={[{ required: true, message: 'Please select a role.' }]}
          >
            <Select
              loading={isLoadingRoles}
              placeholder="Select a role"
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
            label="Effective From"
            rules={[{ required: true, message: 'Effective From date is required.' }]}
            initialValue={dayjs()}
          >
            <DatePicker showTime style={{ width: '100%' }} data-testid="effective-from-picker" />
          </Form.Item>

          <Form.Item
            name="effectiveTo"
            label="Effective To"
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
        title="Deactivate Role Assignment"
        onCancel={handleDeactivateCancel}
        onOk={handleDeactivateConfirm}
        confirmLoading={deactivateMutation.isPending}
        okButtonProps={{ danger: true }}
        okText="Deactivate"
        destroyOnClose
        data-testid="deactivate-assignment-modal"
      >
        <p>Are you sure you want to deactivate the role assignment for <strong>{deactivatingAssignment?.roleName}</strong>?</p>
        <Alert type="warning" message="This action will immediately revoke the role from the user." style={{ marginBottom: 16 }} />
        {actionError && (
          <Alert type="error" message={actionError} data-testid="deactivate-error" />
        )}
      </Modal>
    </div>
  );
};

export default UserRoleAssignmentsPage;
