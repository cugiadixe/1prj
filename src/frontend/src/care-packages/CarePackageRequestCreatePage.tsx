import React, { useRef, useState } from 'react';
import { Alert, Button, DatePicker, Form, Input, InputNumber, Space, Typography, notification } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useCreateCarePackageRequest } from './hooks';
import { getErrorMessage } from './errorMessages';
import RemoteSelect, { type RemoteSelectOption } from '../components/RemoteSelect';
import { usePermissions } from '../auth/AuthProvider';
import { searchCustomers } from '../customers/customersApi';
import { searchServiceTypes } from '../services/serviceTypesApi';
import { searchGraves } from '../graves/gravesApi';
import type { CreateCarePackageRequest } from './types';
import dayjs from 'dayjs';

const { Title } = Typography;

const CarePackageRequestCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const createMutation = useCreateCarePackageRequest();
  const [form] = Form.useForm();
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const selectedCustomerId = Form.useWatch('customerId', form) as number | undefined;
  // Số cốt của từng phần mộ (id → cốt), gom từ kết quả tìm kiếm để tự điền khi chọn phần mộ.
  const graveCotByIdRef = useRef<Record<number, number>>({});

  if (!hasPermission('CARE_PACKAGE_CREATE')) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền tạo yêu cầu gói chăm sóc."
        data-testid="permission-denied"
      />
    );
  }

  const fetchCustomers = async (search: string): Promise<RemoteSelectOption[]> => {
    const res = await searchCustomers({ search, pageSize: 20 });
    return res.items.map((c) => ({
      value: c.id,
      label: `${c.fullName} (${c.customerCode})`,
    }));
  };

  const fetchServiceTypes = async (search: string): Promise<RemoteSelectOption[]> => {
    const res = await searchServiceTypes({ page: 1, pageSize: 100 });
    const term = search.trim().toLowerCase();
    return res.items
      .filter((st) => st.isCarePackage && st.isActive)
      .filter((st) => !term || `${st.name} ${st.code}`.toLowerCase().includes(term))
      .map((st) => ({
        value: st.id,
        label: `${st.name} (${st.code}) — ${st.pricingBasis === 'PER_GRAVE' ? 'theo phần mộ' : 'theo cốt'}`,
      }));
  };

  const fetchGraves = async (search: string): Promise<RemoteSelectOption[]> => {
    // Chỉ hiển thị phần mộ DO KHÁCH ĐÃ CHỌN SỞ HỮU.
    if (!selectedCustomerId) return [];
    const res = await searchGraves({ search, ownerCustomerId: selectedCustomerId, pageSize: 50 });
    res.items.forEach((g) => {
      graveCotByIdRef.current[g.id] = g.cotCount;
    });
    return res.items.map((g) => ({
      value: g.id,
      label: `${g.graveCode} — Khu ${g.zone} • ${g.cotCount} cốt`,
    }));
  };

  const onValuesChange = (changed: any) => {
    // Đổi khách hàng → phần mộ cũ không còn hợp lệ, reset phần mộ + số cốt.
    if ('customerId' in changed) {
      form.setFieldsValue({ graveId: undefined, cotCount: undefined });
    }
    if ('graveId' in changed) {
      const cot = changed.graveId != null ? graveCotByIdRef.current[changed.graveId] : undefined;
      form.setFieldValue('cotCount', cot);
    }
  };

  const onFinish = async (values: any) => {
    setErrorMsg(null);
    const payload: CreateCarePackageRequest = {
      customerId: values.customerId,
      serviceTypeId: values.serviceTypeId,
      saleDate: values.saleDate ? values.saleDate.format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD'),
      discountAmount: values.discountAmount || 0,
      discountReason: values.discountReason || undefined,
      item: {
        graveId: values.graveId,
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
        onValuesChange={onValuesChange}
        data-testid="care-package-create-form"
        style={{ maxWidth: 600 }}
        initialValues={{ saleDate: dayjs(), servicePeriodStartDate: dayjs(), discountAmount: 0 }}
      >
        <Form.Item
          name="customerId"
          label="Khách hàng"
          rules={[{ required: true, message: 'Vui lòng chọn khách hàng' }]}
        >
          <RemoteSelect
            placeholder="Tìm theo tên hoặc mã khách hàng"
            queryKey={['care-package-customers']}
            fetchOptions={fetchCustomers}
            data-testid="input-customerId"
          />
        </Form.Item>

        <Form.Item
          name="serviceTypeId"
          label="Gói dịch vụ (chăm sóc)"
          rules={[{ required: true, message: 'Vui lòng chọn gói dịch vụ' }]}
        >
          <RemoteSelect
            placeholder="Chọn gói chăm sóc từ danh mục"
            queryKey={['care-package-service-types']}
            fetchOptions={fetchServiceTypes}
            data-testid="input-serviceTypeId"
          />
        </Form.Item>

        <Form.Item
          name="saleDate"
          label="Ngày bán"
          rules={[{ required: true, message: 'Vui lòng chọn ngày bán' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-saleDate" />
        </Form.Item>

        <Form.Item
          name="graveId"
          label="Phần mộ"
          rules={[{ required: true, message: 'Vui lòng chọn phần mộ' }]}
          extra={!selectedCustomerId ? 'Chọn khách hàng trước để hiển thị phần mộ của họ.' : undefined}
        >
          <RemoteSelect
            placeholder="Chọn phần mộ khách sở hữu"
            disabled={!selectedCustomerId}
            queryKey={['care-package-graves', selectedCustomerId]}
            fetchOptions={fetchGraves}
            data-testid="input-graveId"
          />
        </Form.Item>

        <Form.Item
          name="cotCount"
          label="Số lượng cốt"
          tooltip="Lấy tự động từ phần mộ đã chọn, không sửa được. Giá tính theo cốt hay theo phần mộ tùy định nghĩa gói dịch vụ."
        >
          <InputNumber style={{ width: '100%' }} disabled data-testid="input-cotCount" placeholder="Chọn phần mộ để tự điền" />
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
