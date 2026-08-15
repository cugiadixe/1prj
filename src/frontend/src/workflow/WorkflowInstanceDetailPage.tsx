import React, { useState } from 'react';
import {
  Alert,
  Button,
  Descriptions,
  Form,
  Input,
  InputNumber,
  Modal,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { useAuth, usePermissions } from '../auth/AuthProvider';
import {
  approveStep,
  getInstance,
  reassignStep,
  rejectStep,
  resubmitInstance,
  retryExecution,
  returnStep,
  withdrawInstance,
} from './workflowRuntimeApi';
import { getErrorMessage, isConcurrencyError, isPermissionDenied } from './errorMessages';
import type { WorkflowInstanceStep } from './types';
import WorkflowActionHistoryPanel from './WorkflowActionHistoryPanel';
import WorkflowRejectDialog from './WorkflowRejectDialog';
import WorkflowRetryExecutionButton from './WorkflowRetryExecutionButton';
import { formatUtcDateTime } from '../utils/datetime';
import { INSTANCE_STATUS_COLORS, INSTANCE_STATUS_LABELS } from './instanceStatus';

const { Title } = Typography;

const STEP_STATUS_COLORS: Record<string, string> = {
  PENDING: 'blue',
  WAITING: 'default',
  APPROVED: 'green',
  RETURNED: 'orange',
  REJECTED: 'volcano',
  CANCELLED: 'default',
};

const STEP_STATUS_LABELS: Record<string, string> = {
  PENDING: 'Chờ xử lý',
  WAITING: 'Chờ tới lượt',
  APPROVED: 'Đã duyệt',
  RETURNED: 'Trả lại',
  REJECTED: 'Từ chối',
  CANCELLED: 'Đã hủy',
};

const WorkflowInstanceDetailPage: React.FC = () => {
  const { instanceId } = useParams<{ instanceId: string }>();
  const { user } = useAuth();
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);
  const [showConcurrencyRefresh, setShowConcurrencyRefresh] = useState(false);
  const [approveModalOpen, setApproveModalOpen] = useState(false);
  const [returnModalOpen, setReturnModalOpen] = useState(false);
  const [reassignModalOpen, setReassignModalOpen] = useState(false);
  const [activeStepId, setActiveStepId] = useState<number | null>(null);
  const [activeStepRowVersion, setActiveStepRowVersion] = useState<string>('');
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [approveForm] = Form.useForm();
  const [returnForm] = Form.useForm();
  const [reassignForm] = Form.useForm();

  const numericId = Number(instanceId);

  const { data: instance, isLoading, error, refetch } = useQuery({
    queryKey: ['workflow-instance', numericId],
    queryFn: () => getInstance(numericId),
    enabled: !isNaN(numericId),
  });

  const handleError = (err: unknown) => {
    if (isConcurrencyError(err)) {
      setShowConcurrencyRefresh(true);
    }
    setActionError(getErrorMessage(err));
  };

  const handleRefresh = async () => {
    setShowConcurrencyRefresh(false);
    setActionError(null);
    await refetch();
  };

  const onSuccess = () => {
    queryClient.invalidateQueries({ queryKey: ['workflow-instance', numericId] });
    queryClient.invalidateQueries({ queryKey: ['workflow-my-approvals'] });
    setActionError(null);
    setShowConcurrencyRefresh(false);
  };

  const approveMutation = useMutation({
    mutationFn: (vals: { reason?: string; comment?: string }) =>
      approveStep(numericId, activeStepId!, {
        reason: vals.reason || null,
        comment: vals.comment || null,
        targetVersion: activeStepRowVersion,
      }),
    onSuccess: () => { onSuccess(); setApproveModalOpen(false); approveForm.resetFields(); },
    onError: handleError,
  });

  const returnMutation = useMutation({
    mutationFn: (vals: { reason: string; comment?: string }) =>
      returnStep(numericId, activeStepId!, {
        reason: vals.reason,
        comment: vals.comment || null,
        targetVersion: activeStepRowVersion,
      }),
    onSuccess: () => { onSuccess(); setReturnModalOpen(false); returnForm.resetFields(); },
    onError: handleError,
  });

  const resubmitMutation = useMutation({
    mutationFn: () => resubmitInstance(numericId, instance!.rowVersion),
    onSuccess,
    onError: handleError,
  });

  const withdrawMutation = useMutation({
    mutationFn: () => withdrawInstance(numericId, instance!.rowVersion),
    onSuccess,
    onError: handleError,
  });

  const reassignMutation = useMutation({
    mutationFn: (vals: { newAssigneeUserId: number; reason: string }) =>
      reassignStep(numericId, activeStepId!, {
        newAssigneeUserId: vals.newAssigneeUserId,
        reason: vals.reason,
        targetVersion: activeStepRowVersion,
      }),
    onSuccess: () => { onSuccess(); setReassignModalOpen(false); reassignForm.resetFields(); },
    onError: handleError,
  });

  const rejectMutation = useMutation({
    mutationFn: (vals: { reason: string; comment?: string }) =>
      rejectStep(numericId, activeStepId!, {
        reason: vals.reason,
        comment: vals.comment || null,
        targetVersion: activeStepRowVersion,
      }),
    onSuccess: () => {
      onSuccess();
      queryClient.invalidateQueries({ queryKey: ['workflow-instance-actions', numericId] });
      setRejectModalOpen(false);
    },
    onError: handleError,
  });

  const retryMutation = useMutation({
    mutationFn: () => retryExecution(numericId),
    onSuccess: () => {
      onSuccess();
      queryClient.invalidateQueries({ queryKey: ['workflow-instance-actions', numericId] });
    },
    onError: handleError,
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem phiên xử lý quy trình này."
        data-testid="permission-denied"
      />
    );
  }

  if (isLoading) return <Spin data-testid="instance-loading" />;

  if (error && !isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(error)}
        data-testid="instance-error"
      />
    );
  }

  if (!instance) return null;

  const currentUserId = user?.userId;
  const isRequester = currentUserId === instance.requesterId;
  const canResubmit = isRequester && instance.instanceStatus === 'RETURNED';
  const canWithdraw = isRequester && (instance.instanceStatus === 'PENDING_APPROVAL' || instance.instanceStatus === 'RETURNED');
  // Cho phép chạy lại cả hồ sơ kẹt ở "Chờ thực thi"/"Đang thực thi" (hồ sơ mồ côi), không chỉ "Thất bại".
  const canRetry =
    hasPermission('WORKFLOW_RETRY_EXECUTION') &&
    ['FAILED', 'PENDING_EXECUTION', 'EXECUTING'].includes(instance.instanceStatus);
  const canReject = hasPermission('WORKFLOW_REJECT');

  const openApproveModal = (step: WorkflowInstanceStep) => {
    setActiveStepId(step.id);
    setActiveStepRowVersion(step.rowVersion);
    approveForm.resetFields();
    setApproveModalOpen(true);
  };

  const openReturnModal = (step: WorkflowInstanceStep) => {
    setActiveStepId(step.id);
    setActiveStepRowVersion(step.rowVersion);
    returnForm.resetFields();
    setReturnModalOpen(true);
  };

  const openReassignModal = (step: WorkflowInstanceStep) => {
    setActiveStepId(step.id);
    setActiveStepRowVersion(step.rowVersion);
    reassignForm.resetFields();
    setReassignModalOpen(true);
  };

  const openRejectModal = (step: WorkflowInstanceStep) => {
    setActiveStepId(step.id);
    setActiveStepRowVersion(step.rowVersion);
    setRejectModalOpen(true);
  };

  const handleResubmit = () => {
    Modal.confirm({
      title: 'Gửi lại yêu cầu',
      content: 'Bạn có chắc chắn muốn gửi lại yêu cầu này để phê duyệt?',
      onOk: () => resubmitMutation.mutate(),
    });
  };

  const handleWithdraw = () => {
    Modal.confirm({
      title: 'Rút yêu cầu',
      content: 'Bạn có chắc chắn muốn rút yêu cầu này? Thao tác này sẽ hủy tất cả các bước đang chờ.',
      onOk: () => withdrawMutation.mutate(),
    });
  };

  const stepColumns = [
    { title: 'Thứ tự', dataIndex: 'stepOrder', key: 'stepOrder' },
    { title: 'Bước', dataIndex: 'stepName', key: 'stepName' },
    { title: 'Vòng', dataIndex: 'roundNo', key: 'roundNo' },
    {
      title: 'Trạng thái',
      dataIndex: 'stepStatus',
      key: 'stepStatus',
      render: (val: string) => <Tag color={STEP_STATUS_COLORS[val] ?? 'default'}>{STEP_STATUS_LABELS[val] ?? val}</Tag>,
    },
    {
      title: 'Người được phân công',
      key: 'assignees',
      render: (_: unknown, record: WorkflowInstanceStep) =>
        record.assignees.map(a => `${a.userName ?? `Người dùng ${a.userId}`} (${a.approverSourceType})`).join(', ') || '—',
    },
    {
      title: 'Người duyệt',
      key: 'completedBy',
      render: (_: unknown, record: WorkflowInstanceStep) =>
        record.completedBy != null ? (record.completedByName ?? `Người dùng ${record.completedBy}`) : '—',
    },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, record: WorkflowInstanceStep) => {
        if (record.stepStatus !== 'PENDING') return null;
        const isAssignee = record.assignees.some(a => a.userId === currentUserId);
        const canApprove = isAssignee && !isRequester;
        const canReturn = isAssignee;
        const canReassign = hasPermission('WORKFLOW_REASSIGN_PENDING');

        return (
          <Space>
            {canApprove && (
              <Button size="small" type="primary" onClick={() => openApproveModal(record)} data-testid={`approve-btn-${record.id}`}>
                Phê duyệt
              </Button>
            )}
            {canReturn && (
              <Button size="small" onClick={() => openReturnModal(record)} data-testid={`return-btn-${record.id}`}>
                Trả lại
              </Button>
            )}
            {canReject && isAssignee && !isRequester && (
              <Button size="small" danger onClick={() => openRejectModal(record)} data-testid={`reject-btn-${record.id}`}>
                Từ chối
              </Button>
            )}
            {canReassign && (
              <Button size="small" onClick={() => openReassignModal(record)} data-testid={`reassign-btn-${record.id}`}>
                Phân công lại
              </Button>
            )}
          </Space>
        );
      },
    },
  ];

  return (
    <div data-testid="instance-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Space>
          <Title level={4} style={{ margin: 0 }}>Phiên xử lý quy trình #{instance.id}</Title>
          <Tag color={INSTANCE_STATUS_COLORS[instance.instanceStatus] ?? 'default'} data-testid="instance-status-tag">
            {INSTANCE_STATUS_LABELS[instance.instanceStatus] ?? instance.instanceStatus}
          </Tag>
        </Space>
        <Space>
          {canResubmit && (
            <Button onClick={handleResubmit} loading={resubmitMutation.isPending} data-testid="resubmit-btn">
              Gửi lại
            </Button>
          )}
          {canWithdraw && (
            <Button danger onClick={handleWithdraw} loading={withdrawMutation.isPending} data-testid="withdraw-btn">
              Rút yêu cầu
            </Button>
          )}
          {canRetry && (
            <WorkflowRetryExecutionButton
              loading={retryMutation.isPending}
              onRetry={() => retryMutation.mutate()}
            />
          )}
        </Space>
      </Space>

      <Alert
        type="info"
        message="Phiên xử lý này sử dụng bản chụp cố định của phiên bản quy trình tại thời điểm tạo. Các thay đổi đối với định nghĩa quy trình sẽ không ảnh hưởng đến phiên xử lý này."
        style={{ marginBottom: 16 }}
        data-testid="version-snapshot-notice"
      />

      {actionError && (
        <Alert
          type="error"
          message={actionError}
          closable={!showConcurrencyRefresh}
          onClose={() => setActionError(null)}
          style={{ marginBottom: 16 }}
          data-testid="action-error"
          action={
            showConcurrencyRefresh ? (
              <Button size="small" type="primary" onClick={handleRefresh} data-testid="refresh-btn">
                Tải lại
              </Button>
            ) : undefined
          }
        />
      )}

      <Descriptions bordered column={2} style={{ marginBottom: 16 }} data-testid="instance-metadata">
        <Descriptions.Item label="Mã quy trình">{instance.processCode}</Descriptions.Item>
        <Descriptions.Item label="Đối tượng">
          {instance.businessEntityLabel ?? `${instance.businessEntityType} #${instance.businessEntityId}`}
        </Descriptions.Item>
        <Descriptions.Item label="ID công ty">{instance.companyId ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Người yêu cầu">{instance.requesterName ?? `Người dùng ${instance.requesterId}`}</Descriptions.Item>
        <Descriptions.Item label="Vòng">{instance.roundNo}</Descriptions.Item>
        <Descriptions.Item label="ID phiên bản">{instance.workflowVersionId}</Descriptions.Item>
        <Descriptions.Item label="Đã tạo">{formatUtcDateTime(instance.createdAt)}</Descriptions.Item>
      </Descriptions>

      <Title level={5} style={{ marginBottom: 8 }}>Các bước</Title>

      {instance.steps.length === 0 ? (
        <Alert type="info" message="Không có bước nào." data-testid="steps-empty" />
      ) : (
        <Table
          dataSource={instance.steps}
          columns={stepColumns}
          rowKey="id"
          pagination={false}
          data-testid="steps-table"
        />
      )}

      {/* Approve Modal */}
      <Modal
        title="Phê duyệt bước"
        open={approveModalOpen}
        onCancel={() => setApproveModalOpen(false)}
        onOk={() => approveForm.submit()}
        confirmLoading={approveMutation.isPending}
        data-testid="approve-modal"
      >
        <p>Bạn có chắc chắn muốn phê duyệt bước này?</p>
        <Form form={approveForm} layout="vertical" onFinish={(vals) => approveMutation.mutate(vals)}>
          <Form.Item name="reason" label="Lý do (tùy chọn)">
            <Input.TextArea rows={2} data-testid="approve-reason" />
          </Form.Item>
          <Form.Item name="comment" label="Ghi chú (tùy chọn)">
            <Input.TextArea rows={2} data-testid="approve-comment" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Return Modal */}
      <Modal
        title="Trả lại bước"
        open={returnModalOpen}
        onCancel={() => setReturnModalOpen(false)}
        onOk={() => returnForm.submit()}
        confirmLoading={returnMutation.isPending}
        data-testid="return-modal"
      >
        <p>Bạn có chắc chắn muốn trả lại yêu cầu này? Cần nhập lý do.</p>
        <Form form={returnForm} layout="vertical" onFinish={(vals) => returnMutation.mutate(vals)}>
          <Form.Item name="reason" label="Lý do" rules={[{ required: true, message: 'Lý do là bắt buộc' }]}>
            <Input.TextArea rows={2} data-testid="return-reason" />
          </Form.Item>
          <Form.Item name="comment" label="Ghi chú (tùy chọn)">
            <Input.TextArea rows={2} data-testid="return-comment" />
          </Form.Item>
        </Form>
      </Modal>

      <WorkflowActionHistoryPanel instanceId={numericId} />

      {/* Reject Modal */}
      <WorkflowRejectDialog
        open={rejectModalOpen}
        loading={rejectMutation.isPending}
        onCancel={() => setRejectModalOpen(false)}
        onSubmit={(vals) => rejectMutation.mutate(vals)}
      />

      {/* Reassign Modal */}
      <Modal
        title="Phân công lại bước"
        open={reassignModalOpen}
        onCancel={() => setReassignModalOpen(false)}
        onOk={() => reassignForm.submit()}
        confirmLoading={reassignMutation.isPending}
        data-testid="reassign-modal"
      >
        <Form form={reassignForm} layout="vertical" onFinish={(vals) => reassignMutation.mutate(vals)}>
          <Form.Item name="newAssigneeUserId" label="ID người được phân công mới" rules={[{ required: true, message: 'ID người dùng là bắt buộc' }]}>
            <InputNumber min={1} style={{ width: '100%' }} data-testid="reassign-user-id" />
          </Form.Item>
          <Form.Item name="reason" label="Lý do" rules={[{ required: true, message: 'Lý do là bắt buộc' }]}>
            <Input.TextArea rows={2} data-testid="reassign-reason" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default WorkflowInstanceDetailPage;
