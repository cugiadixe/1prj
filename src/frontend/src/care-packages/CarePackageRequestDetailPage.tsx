import React, { useState } from 'react';
import { Alert, Button, Descriptions, Modal, Input, Select, Space, Spin, Table, Tag, Typography, notification } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import {
  useCarePackageRequest,
  useSubmitCarePackageRequest,
  useApproveCarePackageRequest,
  useRejectCarePackageRequest,
  useCreateCarePackagePayment,
  useCarePackagePaymentStatus,
  useActivateCarePackageRequest,
} from './hooks';
import type { CarePackageRequestItemDto } from './types';

const { Title } = Typography;

const statusColors: Record<string, string> = {
  Draft: 'default',
  PendingApproval: 'orange',
  PaymentEligible: 'blue',
  PendingPayment: 'purple',
  Paid: 'cyan',
  Active: 'green',
  Rejected: 'red',
};

const CarePackageRequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();

  const requestId = parseInt(id || '0', 10);
  const { data, isLoading, error } = useCarePackageRequest(requestId);

  const submitMutation = useSubmitCarePackageRequest();
  const approveMutation = useApproveCarePackageRequest();
  const rejectMutation = useRejectCarePackageRequest();
  const createPaymentMutation = useCreateCarePackagePayment();
  const activateMutation = useActivateCarePackageRequest();

  const showPaymentStatus = !!data?.paymentTransactionId;
  const { data: paymentStatus } = useCarePackagePaymentStatus(requestId, showPaymentStatus);

  const [isApproveModalOpen, setIsApproveModalOpen] = useState(false);
  const [approveComment, setApproveComment] = useState('');
  const [isRejectModalOpen, setIsRejectModalOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState('CASH');
  const [actionError, setActionError] = useState<string | null>(null);

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view this request."
        data-testid="permission-denied"
      />
    );
  }

  if (error) {
    return <Alert type="error" message={getErrorMessage(error)} data-testid="detail-error" />;
  }

  if (isLoading || !data) {
    return <Spin data-testid="detail-loading" />;
  }

  const handleAction = async (actionFn: () => Promise<any>, successMessage: string) => {
    setActionError(null);
    try {
      await actionFn();
      notification.success({ message: successMessage });
    } catch (err) {
      const msg = getErrorMessage(err);
      setActionError(msg);
      notification.error({ message: msg });
    }
  };

  const onSubmit = () => handleAction(
    () => submitMutation.mutateAsync(requestId),
    'Request submitted for approval'
  );

  const onApprove = () => {
    handleAction(
      () => approveMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, comment: approveComment },
      }),
      'Request approved'
    );
    setIsApproveModalOpen(false);
  };

  const onReject = () => {
    handleAction(
      () => rejectMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, reason: rejectReason },
      }),
      'Request rejected'
    );
    setIsRejectModalOpen(false);
  };

  const onCreatePayment = () => {
    handleAction(
      () => createPaymentMutation.mutateAsync({ id: requestId, data: { paymentMethod } }),
      'Payment draft created'
    );
    setIsPaymentModalOpen(false);
  };

  const onActivate = () => handleAction(
    () => activateMutation.mutateAsync(requestId),
    'Care package activated'
  );

  const canSubmit = data.status === 'Draft' && data.requiresApproval && hasPermission('CARE_PACKAGE_CREATE', 'COMPANY');
  const canApprove = data.status === 'PendingApproval' && hasPermission('CARE_PACKAGE_APPROVE', 'COMPANY');
  const canReject = data.status === 'PendingApproval' && hasPermission('CARE_PACKAGE_REJECT', 'COMPANY');
  const canCreatePayment = data.status === 'PaymentEligible' && hasPermission('CARE_PACKAGE_CREATE_PAYMENT', 'COMPANY');
  const canActivate = data.status === 'Paid' && hasPermission('CARE_PACKAGE_CREATE', 'COMPANY');

  const itemColumns = [
    { title: 'Grave ID', dataIndex: 'graveId', key: 'graveId', render: (v: string | null) => v || '—' },
    { title: 'Cot Count', dataIndex: 'cotCountSnapshot', key: 'cotCountSnapshot' },
    {
      title: 'Service Period',
      key: 'period',
      render: (_: any, record: CarePackageRequestItemDto) =>
        `${new Date(record.servicePeriodStartDate).toLocaleDateString()} — ${new Date(record.servicePeriodEndDate).toLocaleDateString()}`,
    },
    {
      title: 'Unit Price',
      dataIndex: 'unitPriceSnapshot',
      key: 'unitPriceSnapshot',
      render: (v: number) => v.toLocaleString('vi-VN') + ' VND',
    },
    {
      title: 'Line Subtotal',
      dataIndex: 'lineSubtotal',
      key: 'lineSubtotal',
      render: (v: number) => v.toLocaleString('vi-VN') + ' VND',
    },
    { title: 'Notes', dataIndex: 'notes', key: 'notes', render: (v: string | null) => v || '—' },
  ];

  return (
    <div data-testid="care-package-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Care Package Request #{data.id}</Title>
        <Button onClick={() => navigate('/care-packages')}>Back to List</Button>
      </Space>

      {actionError && (
        <Alert type="error" message={actionError} style={{ marginBottom: 16 }} data-testid="action-error" />
      )}

      <Space style={{ marginBottom: 16 }}>
        {canSubmit && (
          <Button type="primary" onClick={onSubmit} loading={submitMutation.isPending} data-testid="btn-submit">
            Submit for Approval
          </Button>
        )}
        {canApprove && (
          <Button type="primary" onClick={() => setIsApproveModalOpen(true)} data-testid="btn-approve">
            Approve
          </Button>
        )}
        {canReject && (
          <Button danger onClick={() => setIsRejectModalOpen(true)} data-testid="btn-reject">
            Reject
          </Button>
        )}
        {canCreatePayment && (
          <Button type="primary" onClick={() => setIsPaymentModalOpen(true)} loading={createPaymentMutation.isPending} data-testid="btn-create-payment">
            Create Payment
          </Button>
        )}
        {canActivate && (
          <Button type="primary" onClick={onActivate} loading={activateMutation.isPending} data-testid="btn-activate">
            Activate
          </Button>
        )}
        {data.paymentTransactionId && (
          <Button type="link" onClick={() => navigate(`/payments/${data.paymentTransactionId}`)} data-testid="btn-view-payment">
            View Payment
          </Button>
        )}
      </Space>

      <Descriptions bordered column={2} style={{ marginBottom: 24 }}>
        <Descriptions.Item label="Status">
          <Tag color={statusColors[data.status] || 'default'} data-testid="status-badge">{data.status}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Customer ID">{data.customerId}</Descriptions.Item>
        <Descriptions.Item label="Company ID">{data.companyId}</Descriptions.Item>
        <Descriptions.Item label="Service ID">{data.serviceId ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Sale Date">{new Date(data.saleDate).toLocaleDateString()}</Descriptions.Item>
        <Descriptions.Item label="Requires Approval">{data.requiresApproval ? 'Yes' : 'No'}</Descriptions.Item>
        <Descriptions.Item label="Previous Request ID">{data.previousRequestId ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Workflow Instance">
          {data.workflowInstanceId ? (
            <Button type="link" size="small" onClick={() => navigate(`/workflow/instances/${data.workflowInstanceId}`)}>
              #{data.workflowInstanceId}
            </Button>
          ) : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Created By">{data.createdByUserId}</Descriptions.Item>
        <Descriptions.Item label="Created At">{new Date(data.createdAt).toLocaleString()}</Descriptions.Item>
        <Descriptions.Item label="Updated By">{data.updatedByUserId ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Updated At">{data.updatedAt ? new Date(data.updatedAt).toLocaleString() : '—'}</Descriptions.Item>
      </Descriptions>

      <Title level={5}>Line Items</Title>
      <Table
        dataSource={data.items}
        columns={itemColumns}
        rowKey="id"
        pagination={false}
        data-testid="line-items-table"
        style={{ marginBottom: 24 }}
      />

      <Descriptions bordered column={1} style={{ maxWidth: 400, marginBottom: 24 }}>
        <Descriptions.Item label="Subtotal">{data.subtotalAmount.toLocaleString('vi-VN')} VND</Descriptions.Item>
        <Descriptions.Item label="Discount">
          {data.discountAmount > 0
            ? `${data.discountAmount.toLocaleString('vi-VN')} VND${data.discountReason ? ` (${data.discountReason})` : ''}`
            : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Total">
          <strong data-testid="total-amount">{data.totalAmount.toLocaleString('vi-VN')} VND</strong>
        </Descriptions.Item>
      </Descriptions>

      {showPaymentStatus && paymentStatus && (
        <Descriptions bordered column={1} style={{ maxWidth: 400, marginBottom: 24 }}>
          <Descriptions.Item label="Payment Status">
            <Tag data-testid="payment-status-badge">{paymentStatus.status}</Tag>
          </Descriptions.Item>
        </Descriptions>
      )}

      <Modal
        title="Approve Request"
        open={isApproveModalOpen}
        onOk={onApprove}
        onCancel={() => setIsApproveModalOpen(false)}
        confirmLoading={approveMutation.isPending}
      >
        <Input.TextArea
          placeholder="Optional comment"
          value={approveComment}
          onChange={(e) => setApproveComment(e.target.value)}
          data-testid="input-approve-comment"
        />
      </Modal>

      <Modal
        title="Reject Request"
        open={isRejectModalOpen}
        onOk={onReject}
        onCancel={() => setIsRejectModalOpen(false)}
        confirmLoading={rejectMutation.isPending}
      >
        <Input.TextArea
          placeholder="Reason for rejection"
          value={rejectReason}
          onChange={(e) => setRejectReason(e.target.value)}
          data-testid="input-reject-reason"
        />
      </Modal>

      <Modal
        title="Create Payment"
        open={isPaymentModalOpen}
        onOk={onCreatePayment}
        onCancel={() => setIsPaymentModalOpen(false)}
        confirmLoading={createPaymentMutation.isPending}
      >
        <Select
          value={paymentMethod}
          onChange={setPaymentMethod}
          style={{ width: '100%' }}
          data-testid="select-payment-method"
          options={[
            { label: 'Cash', value: 'CASH' },
            { label: 'Transfer', value: 'TRANSFER' },
          ]}
        />
      </Modal>
    </div>
  );
};

export default CarePackageRequestDetailPage;
