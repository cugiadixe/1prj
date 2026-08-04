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
        message="You do not have permission to create card reprint requests."
        data-testid="permission-denied"
      />
    );
  }

  const onFinish = async (values: { cardId: number; reasonCode?: string; notes?: string }) => {
    setErrorMsg(null);
    try {
      const result = await createMutation.mutateAsync(values);
      notification.success({ message: 'Request created successfully' });
      navigate(`/cards/reprints/${result.id}`);
    } catch (err) {
      setErrorMsg(getErrorMessage(err));
    }
  };

  return (
    <div data-testid="card-reprint-create-page">
      <Title level={4}>Create Card Reprint Request</Title>
      
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
          label="Card ID"
          rules={[{ required: true, message: 'Please input the Card ID!' }]}
        >
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-cardId" />
        </Form.Item>

        <Form.Item
          name="reasonCode"
          label="Reason Code"
        >
          <Input data-testid="input-reasonCode" />
        </Form.Item>

        <Form.Item
          name="notes"
          label="Notes"
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
              Create
            </Button>
            <Button onClick={() => navigate('/cards/reprints')} data-testid="cancel-btn">
              Cancel
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </div>
  );
};

export default CardReprintRequestCreatePage;
