import React, { useState } from 'react';
import { Button, Card, DatePicker, Form, Input, InputNumber, Space, Typography, message } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { createDraft } from '../paymentApi';
import { getErrorMessage } from '../errorMessages';

const { Title } = Typography;

const PaymentCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [items, setItems] = useState<any[]>([{ key: Date.now() }]);

  const mutation = useMutation({
    mutationFn: (values: any) => {
      // transform form values to CreatePaymentDraftRequest
      const payload = {
        customerId: values.customerId,
        companyId: values.companyId,
        paymentMethod: values.paymentMethod,
        paymentDate: values.paymentDate.toISOString(),
        notes: values.notes,
        items: items.map((_, i) => ({
          serviceId: values[`serviceId_${i}`],
          amount: values[`amount_${i}`],
          description: values[`description_${i}`],
        })),
      };
      return createDraft(payload);
    },
    onSuccess: (data) => {
      message.success('Draft payment created successfully.');
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      navigate(`/payments/${data.id}`);
    },
    onError: (err) => {
      message.error(getErrorMessage(err));
    },
  });

  const onFinish = (values: any) => {
    mutation.mutate(values);
  };

  const addItem = () => setItems([...items, { key: Date.now() }]);

  return (
    <div data-testid="payment-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Create Draft Payment</Title>
      </Space>

      <Form form={form} layout="vertical" onFinish={onFinish}>
        <Card title="General Information" style={{ marginBottom: 16 }}>
          <Form.Item name="customerId" label="Customer ID" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} data-testid="create-customer-id" />
          </Form.Item>
          <Form.Item name="companyId" label="Company ID" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} data-testid="create-company-id" />
          </Form.Item>
          <Form.Item name="paymentMethod" label="Payment Method" rules={[{ required: true }]}>
            <Input style={{ width: '100%' }} data-testid="create-payment-method" />
          </Form.Item>
          <Form.Item name="paymentDate" label="Payment Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} data-testid="create-payment-date" />
          </Form.Item>
          <Form.Item name="notes" label="Notes">
            <Input.TextArea data-testid="create-notes" />
          </Form.Item>
        </Card>

        <Card title="Payment Items" style={{ marginBottom: 16 }}>
          {items.map((item, index) => (
            <Space key={item.key} style={{ display: 'flex', marginBottom: 8 }} align="baseline">
              <Form.Item name={`serviceId_${index}`} label="Service ID" rules={[{ required: true }]}>
                <InputNumber data-testid={`item-service-id-${index}`} />
              </Form.Item>
              <Form.Item name={`amount_${index}`} label="Amount" rules={[{ required: true }]}>
                <InputNumber data-testid={`item-amount-${index}`} />
              </Form.Item>
              <Form.Item name={`description_${index}`} label="Description">
                <Input data-testid={`item-desc-${index}`} />
              </Form.Item>
            </Space>
          ))}
          <Button type="dashed" onClick={addItem} data-testid="add-item-btn">
            Add Item
          </Button>
        </Card>

        <Space>
          <Button type="primary" htmlType="submit" loading={mutation.isPending} data-testid="submit-payment-btn">
            Create
          </Button>
          <Button onClick={() => navigate('/payments')}>Cancel</Button>
        </Space>
      </Form>
    </div>
  );
};

export default PaymentCreatePage;
