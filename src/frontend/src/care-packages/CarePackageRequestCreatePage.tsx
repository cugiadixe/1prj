import React, { useState } from 'react';
import { Alert, Button, DatePicker, Form, Input, InputNumber, Space, Typography, notification } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useCreateCarePackageRequest } from './hooks';
import { getErrorMessage } from './errorMessages';
import { usePermissions } from '../auth/AuthProvider';
import type { CreateCarePackageRequest } from './types';
import dayjs from 'dayjs';

const { Title } = Typography;

const CarePackageRequestCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const createMutation = useCreateCarePackageRequest();
  const [form] = Form.useForm();
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  if (!hasPermission('CARE_PACKAGE_CREATE', 'COMPANY')) {
    return (
      <Alert
        type="error"
        message="You do not have permission to create care package requests."
        data-testid="permission-denied"
      />
    );
  }

  const onFinish = async (values: any) => {
    setErrorMsg(null);
    const payload: CreateCarePackageRequest = {
      customerId: values.customerId,
      serviceId: values.serviceId || undefined,
      saleDate: values.saleDate ? values.saleDate.format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD'),
      discountAmount: values.discountAmount || 0,
      discountReason: values.discountReason || undefined,
      item: {
        graveId: values.graveId || undefined,
        cotCount: values.cotCount,
        servicePeriodStartDate: values.servicePeriodStartDate
          ? values.servicePeriodStartDate.format('YYYY-MM-DD')
          : dayjs().format('YYYY-MM-DD'),
      },
    };
    try {
      const result = await createMutation.mutateAsync(payload);
      notification.success({ message: 'Care package request created' });
      navigate(`/care-packages/${result.id}`);
    } catch (err) {
      setErrorMsg(getErrorMessage(err));
    }
  };

  return (
    <div data-testid="care-package-create-page">
      <Title level={4}>Create Care Package Request</Title>

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
        data-testid="care-package-create-form"
        style={{ maxWidth: 600 }}
        initialValues={{ saleDate: dayjs(), servicePeriodStartDate: dayjs(), discountAmount: 0 }}
      >
        <Form.Item
          name="customerId"
          label="Customer ID"
          rules={[{ required: true, message: 'Please input the Customer ID' }]}
        >
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-customerId" />
        </Form.Item>

        <Form.Item name="serviceId" label="Service ID">
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-serviceId" />
        </Form.Item>

        <Form.Item
          name="saleDate"
          label="Sale Date"
          rules={[{ required: true, message: 'Please select a sale date' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-saleDate" />
        </Form.Item>

        <Form.Item name="graveId" label="Grave / Care Target ID">
          <Input data-testid="input-graveId" />
        </Form.Item>

        <Form.Item
          name="cotCount"
          label="Number of Cots"
          rules={[{ required: true, message: 'Please input the number of cots' }]}
        >
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-cotCount" />
        </Form.Item>

        <Form.Item
          name="servicePeriodStartDate"
          label="Service Period Start Date"
          rules={[{ required: true, message: 'Please select service period start date' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-servicePeriodStartDate" />
        </Form.Item>

        <Form.Item name="discountAmount" label="Discount Amount (VND)">
          <InputNumber style={{ width: '100%' }} min={0} data-testid="input-discountAmount" />
        </Form.Item>

        <Form.Item
          name="discountReason"
          label="Discount Reason"
          dependencies={['discountAmount']}
          rules={[
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (getFieldValue('discountAmount') > 0 && !value) {
                  return Promise.reject(new Error('Discount reason is required when discount amount is greater than 0'));
                }
                return Promise.resolve();
              },
            }),
          ]}
        >
          <Input.TextArea rows={2} data-testid="input-discountReason" />
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
            <Button onClick={() => navigate('/care-packages')} data-testid="cancel-btn">
              Cancel
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </div>
  );
};

export default CarePackageRequestCreatePage;
