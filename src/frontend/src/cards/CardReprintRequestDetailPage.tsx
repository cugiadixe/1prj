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
    'Request submitted'
  );

  const onApprove = async () => {
    handleAction(
      () => approveMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, comment: approveComment }
      }),
      'Request approved'
    );
    setIsApproveModalOpen(false);
  };

  const onReject = async () => {
    handleAction(
      () => rejectMutation.mutateAsync({
        id: requestId,
        data: { stepId: data.workflowInstanceId || 0, targetVersion: 0, reason: rejectReason }
      }),
      'Request rejected'
    );
    setIsRejectModalOpen(false);
  };

  const onCreatePayment = () => handleAction(
    () => createPaymentMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Payment draft created'
  );

  const onMarkPrinted = () => handleAction(
    () => markPrintedMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Card marked as printed'
  );

  const onMarkReleased = () => handleAction(
    () => markReleasedMutation.mutateAsync({ id: requestId, data: { rowVersion: data.rowVersion } }),
    'Card marked as released'
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
        <Title level={4} style={{ margin: 0 }}>Card Reprint Request #{data.id}</Title>
        <Button onClick={() => navigate('/cards/reprints')}>Back to List</Button>
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
            Submit
          </Button>
        )}
        {canApproveReject && (
          <>
            <Button type="primary" onClick={() => setIsApproveModalOpen(true)} data-testid="btn-approve">
              Approve
            </Button>
            <Button danger onClick={() => setIsRejectModalOpen(true)} data-testid="btn-reject">
              Reject
            </Button>
          </>
        )}
        {canCreatePayment && (
          <Button type="primary" onClick={onCreatePayment} loading={createPaymentMutation.isPending} data-testid="btn-create-payment">
            Create Payment Draft
          </Button>
        )}
        {canMarkPrinted && (
          <Button type="primary" onClick={onMarkPrinted} loading={markPrintedMutation.isPending} data-testid="btn-mark-printed">
            Mark Printed
          </Button>
        )}
        {canMarkReleased && (
          <Button type="primary" onClick={onMarkReleased} loading={markReleasedMutation.isPending} data-testid="btn-mark-released">
            Mark Released
          </Button>
        )}
        {data.paymentTransactionId && (
          <Button type="link" onClick={() => navigate(`/payments/${data.paymentTransactionId}`)} data-testid="btn-view-payment">
            View Payment
          </Button>
        )}
      </Space>

      <Descriptions bordered column={2}>
        <Descriptions.Item label="Status">
          <Tag color={statusColor} data-testid="status-badge">{data.status}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Card ID">{data.cardId}</Descriptions.Item>
        <Descriptions.Item label="Requester ID">{data.requesterId}</Descriptions.Item>
        <Descriptions.Item label="Company ID">{data.companyId}</Descriptions.Item>
        <Descriptions.Item label="Reprint Number">{data.reprintNumber}</Descriptions.Item>
        <Descriptions.Item label="Request Type">{data.requestType}</Descriptions.Item>
        <Descriptions.Item label="Reason Code">{data.reasonCode || '—'}</Descriptions.Item>
        <Descriptions.Item label="Fee Amount">
          {data.feeAmount != null ? `${data.feeAmount} ${data.feeCurrency}` : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Notes" span={2}>{data.notes || '—'}</Descriptions.Item>
      </Descriptions>

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
    </div>
  );
};

export default CardReprintRequestDetailPage;
