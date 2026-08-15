import React, { useState } from 'react';
import { Alert, Button, Card, Descriptions, Modal, Space, Spin, Table, Tag, Typography, message, Input } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermissions } from '../../auth/AuthProvider';
import { getPaymentById, confirmPayment, softDeleteDraft, correctConfirmed } from '../paymentApi';
import { getErrorMessage, isPermissionDenied, isConcurrencyError } from '../errorMessages';

const { Title } = Typography;

const PaymentDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = usePermissions();
  const paymentId = parseInt(id || '0', 10);

  const [deleteModalVisible, setDeleteModalVisible] = useState(false);
  const [correctModalVisible, setCorrectModalVisible] = useState(false);
  const [correctReason, setCorrectReason] = useState('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['payment', paymentId],
    queryFn: () => getPaymentById(paymentId),
    enabled: paymentId > 0,
  });

  const confirmMutation = useMutation({
    mutationFn: (rowVersion: string) => confirmPayment(paymentId, { rowVersion }),
    onSuccess: () => {
      message.success('Xác nhận thanh toán thành công.');
      queryClient.invalidateQueries({ queryKey: ['payment', paymentId] });
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Dữ liệu đã thay đổi kể từ khi bạn bắt đầu. Vui lòng tải lại và thử lại.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  const correctMutation = useMutation({
    mutationFn: ({ reason, rowVersion }: { reason: string; rowVersion: string }) =>
      correctConfirmed(paymentId, { reason, rowVersion }),
    onSuccess: () => {
      message.success('Điều chỉnh thanh toán thành công.');
      setCorrectModalVisible(false);
      setCorrectReason('');
      queryClient.invalidateQueries({ queryKey: ['payment', paymentId] });
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Dữ liệu đã thay đổi kể từ khi bạn bắt đầu. Vui lòng tải lại và thử lại.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (rowVersion: string) => softDeleteDraft(paymentId, { rowVersion }),
    onSuccess: () => {
      message.success('Đã xóa thanh toán nháp.');
      navigate('/payments');
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Dữ liệu đã thay đổi kể từ khi bạn bắt đầu. Vui lòng tải lại và thử lại.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem thanh toán này."
        data-testid="permission-denied"
      />
    );
  }

  if (isLoading) {
    return <Spin data-testid="payment-detail-loading" />;
  }

  if (error || !data) {
    return (
      <Alert
        type="error"
        message={error ? getErrorMessage(error) : 'Không tìm thấy thanh toán'}
        data-testid="payment-detail-error"
      />
    );
  }

  const isDraft = data.status === 'DRAFT';
  const isConfirmed = data.status === 'CONFIRMED';
  const canConfirm = isDraft && hasPermission('PAYMENT_CONFIRM');
  const canDelete = isDraft && hasPermission('PAYMENT_CREATE_DRAFT');
  const canCorrect = isConfirmed && hasPermission('PAYMENT_CORRECT_CONFIRMED');

  const itemColumns = [
    { title: 'Mã dịch vụ', dataIndex: 'serviceId', key: 'serviceId' },
    { title: 'Mã loại', dataIndex: 'serviceTypeCode', key: 'serviceTypeCode' },
    { title: 'Chu kỳ', dataIndex: 'serviceCycleNumber', key: 'serviceCycleNumber' },
    { title: 'Mô tả', dataIndex: 'description', key: 'description' },
    {
      title: 'Số tiền',
      dataIndex: 'amount',
      key: 'amount',
      render: (val: number) => `${val.toLocaleString()} VND`
    },
  ];

  return (
    <div data-testid="payment-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Chi tiết thanh toán: {data.billCode}</Title>
        <Space>
          {canConfirm && (
            <Button
              type="primary"
              data-testid="confirm-payment-btn"
              loading={confirmMutation.isPending}
              onClick={() => confirmMutation.mutate(data.rowVersion)}
            >
              Xác nhận
            </Button>
          )}
          {canDelete && (
            <Button
              danger
              data-testid="delete-draft-btn"
              onClick={() => setDeleteModalVisible(true)}
            >
              Xóa nháp
            </Button>
          )}
          {canCorrect && (
            <Button
              type="default"
              data-testid="correct-payment-btn"
              onClick={() => setCorrectModalVisible(true)}
            >
              Điều chỉnh quản trị
            </Button>
          )}
        </Space>
      </Space>

      <Card title="Thông tin chung" style={{ marginBottom: 16 }}>
        <Descriptions column={2}>
          <Descriptions.Item label="Trạng thái">
            <Tag color={isConfirmed ? 'green' : 'blue'}>{data.status}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Phương thức thanh toán">{data.paymentMethod}</Descriptions.Item>
          <Descriptions.Item label="Ngày thanh toán">{new Date(data.paymentDate).toLocaleDateString('vi-VN')}</Descriptions.Item>
          <Descriptions.Item label="Mã khách hàng">{data.customerId}</Descriptions.Item>
          <Descriptions.Item label="Mã công ty">{data.companyId}</Descriptions.Item>
          <Descriptions.Item label="Tổng số tiền">{`${data.totalAmount.toLocaleString()} VND`}</Descriptions.Item>
          <Descriptions.Item label="Tiền tệ">{data.currencyCode}</Descriptions.Item>
          <Descriptions.Item label="Ngày tạo">{new Date(data.createdAt).toLocaleString('vi-VN')}</Descriptions.Item>
          <Descriptions.Item label="Ghi chú">{data.notes || '—'}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="Các mục thanh toán">
        <Table
          dataSource={data.items}
          columns={itemColumns}
          rowKey="id"
          pagination={false}
          data-testid="payment-items-table"
        />
      </Card>

      <Modal
        title="Xóa thanh toán nháp"
        open={deleteModalVisible}
        onOk={() => {
          deleteMutation.mutate(data.rowVersion);
          setDeleteModalVisible(false);
        }}
        onCancel={() => setDeleteModalVisible(false)}
        okText="Xóa"
        okButtonProps={{ danger: true }}
      >
        <p>Bạn có chắc chắn muốn xóa thanh toán nháp này không?</p>
      </Modal>
      <Modal
        title="Điều chỉnh quản trị"
        open={correctModalVisible}
        onOk={() => {
          if (!correctReason) {
            message.error('Lý do là bắt buộc');
            return;
          }
          correctMutation.mutate({ reason: correctReason, rowVersion: data.rowVersion });
        }}
        confirmLoading={correctMutation.isPending}
        onCancel={() => setCorrectModalVisible(false)}
        okText="Gửi điều chỉnh"
      >
        <p>Nhập lý do cho điều chỉnh quản trị này:</p>
        <Input.TextArea
          value={correctReason}
          onChange={(e) => setCorrectReason(e.target.value)}
          placeholder="Lý do điều chỉnh..."
          data-testid="correction-reason-input"
        />
        <p style={{ marginTop: 8, color: 'gray' }}>Lưu ý: Các thay đổi trường được bỏ qua trong biểu mẫu đơn giản này.</p>
      </Modal>
    </div>
  );
};

export default PaymentDetailPage;
