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
        message="Bạn không có quyền xem yêu cầu này."
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
    'Đã gửi yêu cầu phê duyệt'
  );

  const onApprove = () => {
    handleAction(
      () => approveMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, comment: approveComment },
      }),
      'Đã phê duyệt yêu cầu'
    );
    setIsApproveModalOpen(false);
  };

  const onReject = () => {
    handleAction(
      () => rejectMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, reason: rejectReason },
      }),
      'Đã từ chối yêu cầu'
    );
    setIsRejectModalOpen(false);
  };

  const onCreatePayment = () => {
    handleAction(
      () => createPaymentMutation.mutateAsync({ id: requestId, data: { paymentMethod } }),
      'Đã tạo thanh toán nháp'
    );
    setIsPaymentModalOpen(false);
  };

  const onActivate = () => handleAction(
    () => activateMutation.mutateAsync(requestId),
    'Đã kích hoạt gói chăm sóc'
  );

  const canSubmit = data.status === 'Draft' && data.requiresApproval && hasPermission('CARE_PACKAGE_CREATE');
  const canApprove = data.status === 'PendingApproval' && hasPermission('CARE_PACKAGE_APPROVE');
  const canReject = data.status === 'PendingApproval' && hasPermission('CARE_PACKAGE_REJECT');
  const canCreatePayment = data.status === 'PaymentEligible' && hasPermission('CARE_PACKAGE_CREATE_PAYMENT');
  const canActivate = data.status === 'Paid' && hasPermission('CARE_PACKAGE_CREATE');

  const itemColumns = [
    { title: 'Mã mộ', dataIndex: 'graveId', key: 'graveId', render: (v: string | null) => v || '—' },
    { title: 'Số lượng cốt', dataIndex: 'cotCountSnapshot', key: 'cotCountSnapshot' },
    {
      title: 'Kỳ dịch vụ',
      key: 'period',
      render: (_: any, record: CarePackageRequestItemDto) =>
        `${new Date(record.servicePeriodStartDate).toLocaleDateString('vi-VN')} — ${new Date(record.servicePeriodEndDate).toLocaleDateString('vi-VN')}`,
    },
    {
      title: 'Đơn giá',
      dataIndex: 'unitPriceSnapshot',
      key: 'unitPriceSnapshot',
      render: (v: number) => v.toLocaleString('vi-VN') + ' VND',
    },
    {
      title: 'Thành tiền',
      dataIndex: 'lineSubtotal',
      key: 'lineSubtotal',
      render: (v: number) => v.toLocaleString('vi-VN') + ' VND',
    },
    { title: 'Ghi chú', dataIndex: 'notes', key: 'notes', render: (v: string | null) => v || '—' },
  ];

  return (
    <div data-testid="care-package-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Yêu cầu gói chăm sóc #{data.id}</Title>
        <Button onClick={() => navigate('/care-packages')}>Quay lại danh sách</Button>
      </Space>

      {actionError && (
        <Alert type="error" message={actionError} style={{ marginBottom: 16 }} data-testid="action-error" />
      )}

      <Space style={{ marginBottom: 16 }}>
        {canSubmit && (
          <Button type="primary" onClick={onSubmit} loading={submitMutation.isPending} data-testid="btn-submit">
            Gửi phê duyệt
          </Button>
        )}
        {canApprove && (
          <Button type="primary" onClick={() => setIsApproveModalOpen(true)} data-testid="btn-approve">
            Phê duyệt
          </Button>
        )}
        {canReject && (
          <Button danger onClick={() => setIsRejectModalOpen(true)} data-testid="btn-reject">
            Từ chối
          </Button>
        )}
        {canCreatePayment && (
          <Button type="primary" onClick={() => setIsPaymentModalOpen(true)} loading={createPaymentMutation.isPending} data-testid="btn-create-payment">
            Tạo thanh toán
          </Button>
        )}
        {canActivate && (
          <Button type="primary" onClick={onActivate} loading={activateMutation.isPending} data-testid="btn-activate">
            Kích hoạt
          </Button>
        )}
        {data.paymentTransactionId && (
          <Button type="link" onClick={() => navigate(`/payments/${data.paymentTransactionId}`)} data-testid="btn-view-payment">
            Xem thanh toán
          </Button>
        )}
      </Space>

      <Descriptions bordered column={2} style={{ marginBottom: 24 }}>
        <Descriptions.Item label="Trạng thái">
          <Tag color={statusColors[data.status] || 'default'} data-testid="status-badge">{data.status}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Khách hàng">
          {data.customerName ? `${data.customerName}${data.customerCode ? ` (${data.customerCode})` : ''}` : `#${data.customerId}`}
        </Descriptions.Item>
        <Descriptions.Item label="Gói chăm sóc">{data.serviceName ?? (data.serviceId ? `#${data.serviceId}` : '—')}</Descriptions.Item>
        <Descriptions.Item label="Ngày bán">{new Date(data.saleDate).toLocaleDateString('vi-VN')}</Descriptions.Item>
        <Descriptions.Item label="Cần phê duyệt">{data.requiresApproval ? 'Có' : 'Không'}</Descriptions.Item>
        <Descriptions.Item label="Mã yêu cầu trước">{data.previousRequestId ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Phiên quy trình">
          {data.workflowInstanceId ? (
            <Button type="link" size="small" onClick={() => navigate(`/workflow/instances/${data.workflowInstanceId}`)}>
              #{data.workflowInstanceId}
            </Button>
          ) : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Người tạo">{data.createdByUserId}</Descriptions.Item>
        <Descriptions.Item label="Ngày tạo">{new Date(data.createdAt).toLocaleString('vi-VN')}</Descriptions.Item>
        <Descriptions.Item label="Người cập nhật">{data.updatedByUserId ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Ngày cập nhật">{data.updatedAt ? new Date(data.updatedAt).toLocaleString('vi-VN') : '—'}</Descriptions.Item>
      </Descriptions>

      <Title level={5}>Chi tiết mục</Title>
      <Table
        dataSource={data.items}
        columns={itemColumns}
        rowKey="id"
        pagination={false}
        data-testid="line-items-table"
        style={{ marginBottom: 24 }}
      />

      <Descriptions bordered column={1} style={{ maxWidth: 400, marginBottom: 24 }}>
        <Descriptions.Item label="Tạm tính">{data.subtotalAmount.toLocaleString('vi-VN')} VND</Descriptions.Item>
        <Descriptions.Item label="Giảm giá">
          {data.discountAmount > 0
            ? `${data.discountAmount.toLocaleString('vi-VN')} VND${data.discountReason ? ` (${data.discountReason})` : ''}`
            : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Tổng">
          <strong data-testid="total-amount">{data.totalAmount.toLocaleString('vi-VN')} VND</strong>
        </Descriptions.Item>
      </Descriptions>

      {showPaymentStatus && paymentStatus && (
        <Descriptions bordered column={1} style={{ maxWidth: 400, marginBottom: 24 }}>
          <Descriptions.Item label="Trạng thái thanh toán">
            <Tag data-testid="payment-status-badge">{paymentStatus.status}</Tag>
          </Descriptions.Item>
        </Descriptions>
      )}

      <Modal
        title="Phê duyệt yêu cầu"
        open={isApproveModalOpen}
        onOk={onApprove}
        onCancel={() => setIsApproveModalOpen(false)}
        confirmLoading={approveMutation.isPending}
      >
        <Input.TextArea
          placeholder="Nhận xét (tùy chọn)"
          value={approveComment}
          onChange={(e) => setApproveComment(e.target.value)}
          data-testid="input-approve-comment"
        />
      </Modal>

      <Modal
        title="Từ chối yêu cầu"
        open={isRejectModalOpen}
        onOk={onReject}
        onCancel={() => setIsRejectModalOpen(false)}
        confirmLoading={rejectMutation.isPending}
      >
        <Input.TextArea
          placeholder="Lý do từ chối"
          value={rejectReason}
          onChange={(e) => setRejectReason(e.target.value)}
          data-testid="input-reject-reason"
        />
      </Modal>

      <Modal
        title="Tạo thanh toán"
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
            { label: 'Tiền mặt', value: 'CASH' },
            { label: 'Chuyển khoản', value: 'TRANSFER' },
          ]}
        />
      </Modal>
    </div>
  );
};

export default CarePackageRequestDetailPage;
