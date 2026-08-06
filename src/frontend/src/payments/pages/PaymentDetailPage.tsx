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
      message.success('Payment confirmed successfully.');
      queryClient.invalidateQueries({ queryKey: ['payment', paymentId] });
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Data has changed since you started. Please refresh and try again.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  const correctMutation = useMutation({
    mutationFn: ({ reason, rowVersion }: { reason: string; rowVersion: string }) =>
      correctConfirmed(paymentId, { reason, rowVersion }),
    onSuccess: () => {
      message.success('Payment corrected successfully.');
      setCorrectModalVisible(false);
      setCorrectReason('');
      queryClient.invalidateQueries({ queryKey: ['payment', paymentId] });
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Data has changed since you started. Please refresh and try again.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (rowVersion: string) => softDeleteDraft(paymentId, { rowVersion }),
    onSuccess: () => {
      message.success('Draft payment deleted.');
      navigate('/payments');
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        message.error('Data has changed since you started. Please refresh and try again.');
      } else {
        message.error(getErrorMessage(err));
      }
    }
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view this payment."
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
        message={error ? getErrorMessage(error) : 'Payment not found'}
        data-testid="payment-detail-error"
      />
    );
  }

  const isDraft = data.status === 'DRAFT';
  const isConfirmed = data.status === 'CONFIRMED';
  const canConfirm = isDraft && hasPermission('PAYMENT_CONFIRM', 'GLOBAL');
  const canDelete = isDraft && hasPermission('PAYMENT_CREATE_DRAFT', 'GLOBAL');
  const canCorrect = isConfirmed && hasPermission('PAYMENT_CORRECT_CONFIRMED', 'GLOBAL');

  const itemColumns = [
    { title: 'Service ID', dataIndex: 'serviceId', key: 'serviceId' },
    { title: 'Type Code', dataIndex: 'serviceTypeCode', key: 'serviceTypeCode' },
    { title: 'Cycle', dataIndex: 'serviceCycleNumber', key: 'serviceCycleNumber' },
    { title: 'Description', dataIndex: 'description', key: 'description' },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (val: number) => `${val.toLocaleString()} VND`
    },
  ];

  return (
    <div data-testid="payment-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Payment Details: {data.billCode}</Title>
        <Space>
          {canConfirm && (
            <Button
              type="primary"
              data-testid="confirm-payment-btn"
              loading={confirmMutation.isPending}
              onClick={() => confirmMutation.mutate(data.rowVersion)}
            >
              Confirm
            </Button>
          )}
          {canDelete && (
            <Button
              danger
              data-testid="delete-draft-btn"
              onClick={() => setDeleteModalVisible(true)}
            >
              Delete Draft
            </Button>
          )}
          {canCorrect && (
            <Button
              type="default"
              data-testid="correct-payment-btn"
              onClick={() => setCorrectModalVisible(true)}
            >
              Admin Correction
            </Button>
          )}
        </Space>
      </Space>

      <Card title="General Information" style={{ marginBottom: 16 }}>
        <Descriptions column={2}>
          <Descriptions.Item label="Status">
            <Tag color={isConfirmed ? 'green' : 'blue'}>{data.status}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Payment Method">{data.paymentMethod}</Descriptions.Item>
          <Descriptions.Item label="Payment Date">{new Date(data.paymentDate).toLocaleDateString()}</Descriptions.Item>
          <Descriptions.Item label="Customer ID">{data.customerId}</Descriptions.Item>
          <Descriptions.Item label="Company ID">{data.companyId}</Descriptions.Item>
          <Descriptions.Item label="Total Amount">{`${data.totalAmount.toLocaleString()} VND`}</Descriptions.Item>
          <Descriptions.Item label="Currency">{data.currencyCode}</Descriptions.Item>
          <Descriptions.Item label="Created At">{new Date(data.createdAt).toLocaleString()}</Descriptions.Item>
          <Descriptions.Item label="Notes">{data.notes || '—'}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="Payment Items">
        <Table
          dataSource={data.items}
          columns={itemColumns}
          rowKey="id"
          pagination={false}
          data-testid="payment-items-table"
        />
      </Card>

      <Modal
        title="Delete Draft Payment"
        open={deleteModalVisible}
        onOk={() => {
          deleteMutation.mutate(data.rowVersion);
          setDeleteModalVisible(false);
        }}
        onCancel={() => setDeleteModalVisible(false)}
        okText="Delete"
        okButtonProps={{ danger: true }}
      >
        <p>Are you sure you want to delete this draft payment?</p>
      </Modal>
      <Modal
        title="Admin Correction"
        open={correctModalVisible}
        onOk={() => {
          if (!correctReason) {
            message.error('Reason is required');
            return;
          }
          correctMutation.mutate({ reason: correctReason, rowVersion: data.rowVersion });
        }}
        confirmLoading={correctMutation.isPending}
        onCancel={() => setCorrectModalVisible(false)}
        okText="Submit Correction"
      >
        <p>Enter the reason for this administrative correction:</p>
        <Input.TextArea
          value={correctReason}
          onChange={(e) => setCorrectReason(e.target.value)}
          placeholder="Correction reason..."
          data-testid="correction-reason-input"
        />
        <p style={{ marginTop: 8, color: 'gray' }}>Note: Field modifications are omitted in this simplified form.</p>
      </Modal>
    </div>
  );
};

export default PaymentDetailPage;
