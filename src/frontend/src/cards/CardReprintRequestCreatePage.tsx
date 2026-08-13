import React, { useState } from 'react';
import { Alert, Button, Form, Input, InputNumber, Space, Typography, notification } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useCreateCardReprintRequest } from './hooks';
import { getErrorMessage } from './errorMessages';
import { usePermissions } from '../auth/AuthProvider';

const { Title } = Typography;

const CardReprintRequestCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const createMutation = useCreateCardReprintRequest();
  const [form] = Form.useForm();
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  if (!hasPermission('CARD_REPRINT_REQUEST_CREATE', 'GLOBAL')) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền tạo yêu cầu in lại thẻ."
        data-testid="permission-denied"
      />
    );
  }

  const onFinish = async (values: { cardId: number; reasonCode?: string; notes?: string }) => {
    setErrorMsg(null);
    try {
      const result = await createMutation.mutateAsync(values);
      notification.success({ message: 'Tạo yêu cầu thành công' });
      navigate(`/cards/reprints/${result.id}`);
    } catch (err) {
      setErrorMsg(getErrorMessage(err));
    }
  };

  return (
    <div data-testid="card-reprint-create-page">
      <Title level={4}>Tạo yêu cầu in lại thẻ</Title>

      {errorMsg && (
        <Alert
          type="error"
          message={errorMsg}
          style={{ marginBottom: 16 }}
          data-testid="create-error"
        />
      )}

      <Form
        form={form}
        layout="vertical"
        onFinish={onFinish}
        data-testid="card-reprint-create-form"
        style={{ maxWidth: 600 }}
      >
        <Form.Item
          name="cardId"
          label="Mã thẻ"
          rules={[{ required: true, message: 'Vui lòng nhập mã thẻ!' }]}
        >
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-cardId" />
        </Form.Item>

        <Form.Item
          name="reasonCode"
          label="Mã lý do"
        >
          <Input data-testid="input-reasonCode" />
        </Form.Item>

        <Form.Item
          name="notes"
          label="Ghi chú"
        >
          <Input.TextArea rows={4} data-testid="input-notes" />
        </Form.Item>

        <Form.Item>
          <Space>
            <Button
              type="primary"
              htmlType="submit"
              loading={createMutation.isPending}
              data-testid="submit-btn"
            >
              Tạo
            </Button>
            <Button onClick={() => navigate('/cards/reprints')} data-testid="cancel-btn">
              Hủy
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </div>
  );
};

export default CardReprintRequestCreatePage;
