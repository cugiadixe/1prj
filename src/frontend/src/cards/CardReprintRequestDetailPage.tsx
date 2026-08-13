import React, { useState } from 'react';
import { Alert, Button, Descriptions, Space, Spin, Tag, Typography, Modal, Input, notification } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import {
  useCardReprintRequest,
  useSubmitCardReprintRequest,
  useApproveCardReprintRequest,
  useRejectCardReprintRequest,
  useCreatePaymentForCardReprint,
  useCardReprintPaymentStatus,
  useMarkCardPrinted,
  useMarkCardReleased
} from './hooks';

const { Title } = Typography;

const CardReprintRequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();

  const requestId = parseInt(id || '0', 10);
  const { data, isLoading, error } = useCardReprintRequest(requestId);

  const submitMutation = useSubmitCardReprintRequest();
  const approveMutation = useApproveCardReprintRequest();
  const rejectMutation = useRejectCardReprintRequest();
  const createPaymentMutation = useCreatePaymentForCardReprint();
  const markPrintedMutation = useMarkCardPrinted();
  const markReleasedMutation = useMarkCardReleased();

  // Poll for payment status if PENDING_PAYMENT
  useCardReprintPaymentStatus(requestId, data?.status === 'PENDING_PAYMENT');

  const [rejectReason, setRejectReason] = useState('');
  const [approveComment, setApproveComment] = useState('');
  const [isRejectModalOpen, setIsRejectModalOpen] = useState(false);
  const [isApproveModalOpen, setIsApproveModalOpen] = useState(false);
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

  const handleAction = async (actionFn: () => Promise<void>, successMessage: string) => {
    setActionError(null);
    try {
      await actionFn();
      notification.success({ message: successMessage });
    } catch (err) {
      setActionError(getErrorMessage(err));
      notification.error({ message: getErrorMessage(err) });
    }
  };

  const onSubmit = () => handleAction(
    () => submitMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Đã gửi yêu cầu'
  );

  const onApprove = async () => {
    handleAction(
      () => approveMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, comment: approveComment }
      }),
      'Đã phê duyệt yêu cầu'
    );
    setIsApproveModalOpen(false);
  };

  const onReject = async () => {
    handleAction(
      () => rejectMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, reason: rejectReason }
      }),
      'Đã từ chối yêu cầu'
    );
    setIsRejectModalOpen(false);
  };

  const onCreatePayment = () => handleAction(
    () => createPaymentMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Đã tạo thanh toán nháp'
  );

  const onMarkPrinted = () => handleAction(
    () => markPrintedMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Đã đánh dấu thẻ đã in'
  );

  const onMarkReleased = () => handleAction(
    () => markReleasedMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Đã đánh dấu thẻ đã phát'
  );

  let statusColor = 'default';
  if (data.status === 'PENDING_APPROVAL') statusColor = 'orange';
  else if (data.status === 'APPROVED') statusColor = 'blue';
  else if (data.status === 'REJECTED') statusColor = 'red';
  else if (data.status === 'PENDING_PAYMENT') statusColor = 'purple';
  else if (data.status === 'PAID') statusColor = 'cyan';
  else if (data.status === 'PRINTED') statusColor = 'geekblue';
  else if (data.status === 'RELEASED') statusColor = 'green';

  const canSubmit = data.status === 'DRAFT' && hasPermission('CARD_REPRINT_REQUEST_CREATE', 'GLOBAL');
  const canApproveReject = data.status === 'PENDING_APPROVAL' && hasPermission('CARD_REPRINT_APPROVE', 'GLOBAL');
  const canCreatePayment = data.status === 'APPROVED' && hasPermission('CARD_REPRINT_REQUEST_CREATE', 'GLOBAL');
  const canMarkPrinted = data.status === 'PAID' && hasPermission('CARD_REPRINT_REQUEST_MARK_PRINTED', 'GLOBAL');
  const canMarkReleased = data.status === 'PRINTED' && hasPermission('CARD_REPRINT_REQUEST_MARK_PRINTED', 'GLOBAL');

  return (
    <div data-testid="card-reprint-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Yêu cầu in lại thẻ #{data.id}</Title>
        <Button onClick={() => navigate('/cards/reprints')}>Quay lại danh sách</Button>
      </Space>

      {actionError && (
        <Alert
          type="error"
          message={actionError}
          style={{ marginBottom: 16 }}
          data-testid="action-error"
        />
      )}

      <Space style={{ marginBottom: 16 }}>
        {canSubmit && (
          <Button type="primary" onClick={onSubmit} loading={submitMutation.isPending} data-testid="btn-submit">
            Gửi
          </Button>
        )}
        {canApproveReject && (
          <>
            <Button type="primary" onClick={() => setIsApproveModalOpen(true)} data-testid="btn-approve">
              Phê duyệt
            </Button>
            <Button danger onClick={() => setIsRejectModalOpen(true)} data-testid="btn-reject">
              Từ chối
            </Button>
          </>
        )}
        {canCreatePayment && (
          <Button type="primary" onClick={onCreatePayment} loading={createPaymentMutation.isPending} data-testid="btn-create-payment">
            Tạo thanh toán nháp
          </Button>
        )}
        {canMarkPrinted && (
          <Button type="primary" onClick={onMarkPrinted} loading={markPrintedMutation.isPending} data-testid="btn-mark-printed">
            Đánh dấu đã in
          </Button>
        )}
        {canMarkReleased && (
          <Button type="primary" onClick={onMarkReleased} loading={markReleasedMutation.isPending} data-testid="btn-mark-released">
            Đánh dấu đã phát
          </Button>
        )}
        {data.paymentTransactionId && (
          <Button type="link" onClick={() => navigate(`/payments/${data.paymentTransactionId}`)} data-testid="btn-view-payment">
            Xem thanh toán
          </Button>
        )}
      </Space>

      <Descriptions bordered column={2}>
        <Descriptions.Item label="Trạng thái">
          <Tag color={statusColor} data-testid="status-badge">{data.status}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Mã thẻ">{data.cardId}</Descriptions.Item>
        <Descriptions.Item label="Mã người yêu cầu">{data.requesterId}</Descriptions.Item>
        <Descriptions.Item label="Mã công ty">{data.companyId}</Descriptions.Item>
        <Descriptions.Item label="Số lần in lại">{data.reprintNumber}</Descriptions.Item>
        <Descriptions.Item label="Loại yêu cầu">{data.requestType}</Descriptions.Item>
        <Descriptions.Item label="Mã lý do">{data.reasonCode || '—'}</Descriptions.Item>
        <Descriptions.Item label="Phí">
          {data.feeAmount != null ? `${data.feeAmount} ${data.feeCurrency}` : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Ghi chú" span={2}>{data.notes || '—'}</Descriptions.Item>
      </Descriptions>

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
    </div>
  );
};

export default CardReprintRequestDetailPage;
