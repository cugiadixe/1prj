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
        message="Bạn không có quyền tạo yêu cầu gói chăm sóc."
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
      notification.success({ message: 'Đã tạo yêu cầu gói chăm sóc' });
      navigate(`/care-packages/${result.id}`);
    } catch (err) {
      setErrorMsg(getErrorMessage(err));
    }
  };

  return (
    <div data-testid="care-package-create-page">
      <Title level={4}>Tạo yêu cầu gói chăm sóc</Title>

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
          label="Mã khách hàng"
          rules={[{ required: true, message: 'Vui lòng nhập mã khách hàng' }]}
        >
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-customerId" />
        </Form.Item>

        <Form.Item name="serviceId" label="Mã dịch vụ">
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-serviceId" />
        </Form.Item>

        <Form.Item
          name="saleDate"
          label="Ngày bán"
          rules={[{ required: true, message: 'Vui lòng chọn ngày bán' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-saleDate" />
        </Form.Item>

        <Form.Item name="graveId" label="Mã mộ / Đối tượng chăm sóc">
          <Input data-testid="input-graveId" />
        </Form.Item>

        <Form.Item
          name="cotCount"
          label="Số lượng cốt"
          rules={[{ required: true, message: 'Vui lòng nhập số lượng cốt' }]}
        >
          <InputNumber style={{ width: '100%' }} min={1} data-testid="input-cotCount" />
        </Form.Item>

        <Form.Item
          name="servicePeriodStartDate"
          label="Ngày bắt đầu kỳ dịch vụ"
          rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu kỳ dịch vụ' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-servicePeriodStartDate" />
        </Form.Item>

        <Form.Item name="discountAmount" label="Số tiền giảm giá (VND)">
          <InputNumber style={{ width: '100%' }} min={0} data-testid="input-discountAmount" />
        </Form.Item>

        <Form.Item
          name="discountReason"
          label="Lý do giảm giá"
          dependencies={['discountAmount']}
          rules={[
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (getFieldValue('discountAmount') > 0 && !value) {
                  return Promise.reject(new Error('Lý do giảm giá là bắt buộc khi số tiền giảm giá lớn hơn 0'));
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
              Tạo
            </Button>
            <Button onClick={() => navigate('/care-packages')} data-testid="cancel-btn">
              Hủy
            </Button>
          </Space>
        </Form.Item>
      </Form>
    </div>
  );
};

export default CarePackageRequestCreatePage;
