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
import { adminGroupManagementApi } from '../adminGroupManagement/adminGroupManagementApi';
import { userAdminGroupAssignmentsApi } from './userAdminGroupAssignmentsApi';
import type {
  CreateUserAdminGroupAssignmentRequest,
  DeactivateAssignmentRequest,
} from './userAdminGroupAssignmentsApi';
import {
  getErrorMessage,
  isPermissionDenied,
  isNotFound,
  PERMISSION_DENIED,
  NOT_FOUND,
} from './errorMessages';
import type { UserAdminGroupAssignmentDto } from './userAdminGroupAssignmentsApi';

const { Title, Text } = Typography;

const UserAdminGroupAssignmentsPage: React.FC = () => {
  const { userId } = useParams<{ userId: string }>();
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();

  const userIdNum = userId ? parseInt(userId, 10) : NaN;

  const [isAssignModalVisible, setIsAssignModalVisible] = useState(false);
  const [deactivatingAssignment, setDeactivatingAssignment] = useState<UserAdminGroupAssignmentDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const [form] = Form.useForm();

  // Queries
  const {
    data: assignments,
    isLoading: isLoadingAssignments,
    isError: isAssignmentsError,
    error: assignmentsError,
  } = useQuery({
    queryKey: ['user-admin-group-assignments', userIdNum],
    queryFn: () => userAdminGroupAssignmentsApi.getUserAdminGroupAssignments(userIdNum),
    enabled: !isNaN(userIdNum),
  });

  const { data: adminGroups = [], isLoading: isLoadingAdminGroups } = useQuery({
    queryKey: ['admin-groups'],
    queryFn: () => adminGroupManagementApi.getAdminGroups(),
  });

  // Mutations
  const assignMutation = useMutation({
    mutationFn: async (values: any) => {
      if (isNaN(userIdNum)) return;
      const adminGroupId = values.adminGroupId as number;
      const adminGroup = adminGroups.find(g => g.id === adminGroupId);

      // Enforce company context for COMPANY scoped admin groups
      let companyIdToPass: number | undefined = undefined;
      if (adminGroup?.scopeType === 'COMPANY') {
        if (!currentCompanyId) {
          throw new Error('COMPANY_CONTEXT_REQUIRED');
        }
        companyIdToPass = currentCompanyId;
      }

      const request: CreateUserAdminGroupAssignmentRequest = {
        adminGroupId,
        effectiveFrom: values.effectiveFrom.toISOString(),
        effectiveTo: values.effectiveTo ? values.effectiveTo.toISOString() : null,
      };

      await userAdminGroupAssignmentsApi.assignAdminGroupToUser(userIdNum, request, companyIdToPass);
    },
    onSuccess: () => {
      setIsAssignModalVisible(false);
      form.resetFields();
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ['user-admin-group-assignments', userIdNum] });
    },
    onError: (err: unknown) => {
      const errMessage = getErrorMessage(err);
      if (err instanceof Error && err.message === 'COMPANY_CONTEXT_REQUIRED') {
        setActionError('Phải chọn một công ty cụ thể để phân công nhóm quản trị có phạm vi COMPANY.');
      } else {
        setActionError(errMessage);
      }
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: async (assignment: UserAdminGroupAssignmentDto) => {
      if (isNaN(userIdNum)) return;
      
      // Look up admin group to find scopeType since it's not on the assignment DTO directly? 
      // Wait, let's check UserAdminGroupAssignmentDto: it DOES NOT have scopeType!
      // I need to look up the admin group from `adminGroups` or rely on backend.
      // Q1 used `assignment.scopeType` because UserRoleAssignmentDto HAS scopeType.
      // Ah, wait... Does UserAdminGroupAssignmentDto have scopeType? 
      // No, only groupCode and groupName. 
      // I will find the admin group to check its scope type.
      
      const adminGroup = adminGroups.find(g => g.id === assignment.adminGroupId);
      let companyIdToPass: number | undefined = undefined;
      
      // If we don't have the adminGroup loaded yet, we can't reliably know if it's COMPANY scoped.
      // But we always load adminGroups on this page.
      if (adminGroup?.scopeType === 'COMPANY') {
        if (!currentCompanyId) {
            throw new Error('COMPANY_CONTEXT_REQUIRED');
        }
        companyIdToPass = currentCompanyId;
      }

      const request: DeactivateAssignmentRequest = { rowVersion: assignment.rowVersion };
      await userAdminGroupAssignmentsApi.deactivateUserAdminGroupAssignment(userIdNum, assignment.id, request, companyIdToPass);
    },
    onSuccess: () => {
      setDeactivatingAssignment(null);
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ['user-admin-group-assignments', userIdNum] });
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
  const activeAdminGroups = useMemo(() => adminGroups.filter(g => g.isActive), [adminGroups]);

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

  const handleDeactivateClick = (assignment: UserAdminGroupAssignmentDto) => {
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
      title: 'Nhóm quản trị',
      key: 'adminGroup',
      render: (record: UserAdminGroupAssignmentDto) => (
        <Space direction="vertical" size={0}>
          <Text strong data-testid={`assignment-group-name-${record.id}`}>{record.groupName}</Text>
          <Text type="secondary" data-testid={`assignment-group-code-${record.id}`}>{record.groupCode}</Text>
        </Space>
      ),
    },
    {
      title: 'Phạm vi',
      key: 'scopeType',
      render: (record: UserAdminGroupAssignmentDto) => {
        const adminGroup = adminGroups.find(g => g.id === record.adminGroupId);
        const scopeType = adminGroup?.scopeType || 'UNKNOWN';
        return <Tag data-testid={`assignment-scope-${scopeType}`}>{scopeType}</Tag>;
      },
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
      render: (record: UserAdminGroupAssignmentDto) => {
        const isPastEffectiveTo = record.effectiveTo ? new Date(record.effectiveTo) < new Date() : false;
        // The DTO has assignmentStatus in backend, but we also manually check effectively active
        const effectivelyActive = record.assignmentStatus === 'Active' && !isPastEffectiveTo;
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
      render: (record: UserAdminGroupAssignmentDto) => (
        record.assignmentStatus === 'Active' && (
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
    <div data-testid="user-admin-group-assignments-page">
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <div>
            <Title level={3}>Phân nhóm quản trị người dùng</Title>
            <Text type="secondary" data-testid="user-id-display">Mã người dùng: {userIdNum}</Text>
          </div>
          <Space>
            <Button
              type="primary"
              onClick={handleAssignClick}
              data-testid="assign-admin-group-button"
            >
              Phân nhóm quản trị
            </Button>
          </Space>
        </div>

        <Table
          dataSource={assignments}
          columns={columns}
          rowKey="id"
          loading={isLoadingAssignments || isLoadingAdminGroups}
          pagination={false}
          data-testid="assignments-table"
        />
      </Space>

      {/* Assign Admin Group Modal */}
      <Modal
        open={isAssignModalVisible}
        title="Phân nhóm quản trị"
        onCancel={handleAssignCancel}
        onOk={handleAssignSubmit}
        confirmLoading={assignMutation.isPending}
        destroyOnClose
        data-testid="assign-admin-group-modal"
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="adminGroupId"
            label="Nhóm quản trị"
            rules={[{ required: true, message: 'Vui lòng chọn một nhóm quản trị.' }]}
          >
            <Select
              loading={isLoadingAdminGroups}
              placeholder="Chọn một nhóm quản trị"
              data-testid="assign-admin-group-select"
            >
              {activeAdminGroups.map(group => (
                <Select.Option key={group.id} value={group.id} data-testid={`admin-group-option-${group.id}`}>
                  {group.name} ({group.scopeType})
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
        title="Vô hiệu hóa phân nhóm quản trị"
        onCancel={handleDeactivateCancel}
        onOk={handleDeactivateConfirm}
        confirmLoading={deactivateMutation.isPending}
        okButtonProps={{ danger: true }}
        okText="Vô hiệu hóa"
        destroyOnClose
        data-testid="deactivate-assignment-modal"
      >
        <p>Bạn có chắc chắn muốn vô hiệu hóa phân nhóm quản trị cho <strong>{deactivatingAssignment?.groupName}</strong>?</p>
        <Alert type="warning" message="Hành động này sẽ thu hồi ngay lập tức thành viên nhóm quản trị khỏi người dùng." style={{ marginBottom: 16 }} />
        {actionError && (
          <Alert type="error" message={actionError} data-testid="deactivate-error" />
        )}
      </Modal>
    </div>
  );
};

export default UserAdminGroupAssignmentsPage;
