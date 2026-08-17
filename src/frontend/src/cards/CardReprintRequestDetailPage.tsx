import React from 'react';
import { Alert, Button, Descriptions, Space, Spin, Tag, Typography, notification } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import {
  useCardReprintRequest,
  useSubmitCardReprintRequest,
  usePrintInitialCardReprint,
  useCreatePaymentForCardReprint,
  useCardReprintPaymentStatus,
  useMarkCardPrinted,
  useMarkCardReleased
} from './hooks';

const { Title } = Typography;

const INITIAL_PRINT = 'INITIAL_PRINT';

const CardReprintRequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();

  const requestId = parseInt(id || '0', 10);
  const { data, isLoading, error } = useCardReprintRequest(requestId);

  const submitMutation = useSubmitCardReprintRequest();
  const printInitialMutation = usePrintInitialCardReprint();
  const createPaymentMutation = useCreatePaymentForCardReprint();
  const markPrintedMutation = useMarkCardPrinted();
  const markReleasedMutation = useMarkCardReleased();

  // Poll for payment status if PENDING_PAYMENT
  useCardReprintPaymentStatus(requestId, data?.status === 'PENDING_PAYMENT');

  const [actionError, setActionError] = React.useState<string | null>(null);

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

  const isInitial = data.requestType === INITIAL_PRINT;

  const handleAction = async (actionFn: () => Promise<unknown>, successMessage: string) => {
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
    'Đã gửi yêu cầu trình duyệt'
  );

  const onPrintInitial = () => handleAction(
    () => printInitialMutation.mutateAsync(requestId),
    'Đã in lần đầu (miễn duyệt)'
  );

  const onCreatePayment = () => handleAction(
    () => createPaymentMutation.mutateAsync({ id: requestId, data: { paymentMethod: 'CASH' } }),
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

  // Lần in đầu: in trực tiếp, miễn duyệt. In lại: gửi trình duyệt.
  const canPrintInitial = data.status === 'DRAFT' && isInitial && hasPermission('CARD_REPRINT_REQUEST_MARK_PRINTED');
  const canSubmit = data.status === 'DRAFT' && !isInitial && hasPermission('CARD_REPRINT_REQUEST_CREATE');
  const canOpenApproval = data.status === 'PENDING_APPROVAL' && !!data.workflowInstanceId;
  const canCreatePayment = data.status === 'APPROVED' && hasPermission('CARD_REPRINT_REQUEST_CREATE');
  const canMarkPrinted = data.status === 'PAID' && hasPermission('CARD_REPRINT_REQUEST_MARK_PRINTED');
  const canMarkReleased = data.status === 'PRINTED' && hasPermission('CARD_REPRINT_REQUEST_MARK_PRINTED');

  return (
    <div data-testid="card-reprint-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Yêu cầu in thẻ #{data.id}</Title>
        <Button onClick={() => navigate('/cards/reprints')}>Quay lại danh sách</Button>
      </Space>

      <Alert
        style={{ marginBottom: 16 }}
        type={isInitial ? 'success' : 'warning'}
        showIcon
        data-testid="request-type-banner"
        message={isInitial ? 'In lần đầu — miễn duyệt, miễn phí' : `In lại (lần in thứ ${data.reprintNumber}) — cần duyệt và thu phí`}
        description={
          isInitial
            ? 'Bấm "In lần đầu" để in thẳng, không qua quy trình duyệt.'
            : 'Bấm "Gửi" để trình duyệt. Người duyệt xử lý tại hồ sơ duyệt (mục Quy trình → Chờ duyệt). Sau khi duyệt, tạo thanh toán rồi đánh dấu đã in.'
        }
      />

      {actionError && (
        <Alert
          type="error"
          message={actionError}
          style={{ marginBottom: 16 }}
          data-testid="action-error"
        />
      )}

      <Space style={{ marginBottom: 16 }} wrap>
        {canPrintInitial && (
          <Button type="primary" onClick={onPrintInitial} loading={printInitialMutation.isPending} data-testid="btn-print-initial">
            In lần đầu (miễn duyệt)
          </Button>
        )}
        {canSubmit && (
          <Button type="primary" onClick={onSubmit} loading={submitMutation.isPending} data-testid="btn-submit">
            Gửi
          </Button>
        )}
        {canOpenApproval && (
          <Button type="primary" onClick={() => navigate(`/workflow/instances/${data.workflowInstanceId}`)} data-testid="btn-open-approval">
            Mở hồ sơ duyệt
          </Button>
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
        <Descriptions.Item label="Lần in thứ">{data.reprintNumber}</Descriptions.Item>
        <Descriptions.Item label="Loại yêu cầu">
          {isInitial ? 'In lần đầu (miễn duyệt)' : 'In lại (cần duyệt)'}
        </Descriptions.Item>
        <Descriptions.Item label="Mã lý do">{data.reasonCode || '—'}</Descriptions.Item>
        <Descriptions.Item label="Phí">
          {data.feeAmount != null ? `${data.feeAmount} ${data.feeCurrency}` : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="Ghi chú" span={2}>{data.notes || '—'}</Descriptions.Item>
      </Descriptions>
    </div>
  );
};

export default CardReprintRequestDetailPage;
