import React, { useState } from 'react';
import { Alert, Button, DatePicker, Form, Space, Typography, message, Select } from 'antd';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { createService } from './servicesApi';
import { searchServiceTypes } from './serviceTypesApi';
import { searchCustomers } from '../customers/customersApi';
import { getErrorMessage } from './errorMessages';
import RemoteSelect, { type RemoteSelectOption } from '../components/RemoteSelect';
import type { CreateServiceRequest } from './types';

const { Title } = Typography;

const ServiceCreatePage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const { currentCompanyId } = useCompany();
  const navigate = useNavigate();
  const [form] = Form.useForm();

  const [formError, setFormError] = useState<string | null>(null);

  // For a real app, this might be paginated or an autocomplete.
  // For simplicity, we just fetch the first 100 active service types.
  const { data: serviceTypesData } = useQuery({
    queryKey: ['serviceTypes', 1, 100],
    queryFn: () => searchServiceTypes({ page: 1, pageSize: 100 }),
  });

  const createMutation = useMutation({
    mutationFn: (values: CreateServiceRequest) => createService(values),
    onSuccess: (data) => {
      message.success('Tạo dịch vụ thành công');
      navigate(`/services/${data.id}`);
    },
    onError: (err) => {
      setFormError(getErrorMessage(err));
    },
  });

  const fetchCustomers = async (search: string): Promise<RemoteSelectOption[]> => {
    const res = await searchCustomers({ search, pageSize: 20 });
    return res.items.map((c) => ({ value: c.id, label: `${c.fullName} (${c.customerCode})` }));
  };

  const handleSubmit = (values: any) => {
    setFormError(null);
    createMutation.mutate({
      serviceTypeId: values.serviceTypeId,
      customerId: values.customerId,
      companyId: currentCompanyId!,
      validFrom: values.validFrom.format('YYYY-MM-DD'),
      validTo: values.validTo ? values.validTo.format('YYYY-MM-DD') : undefined,
    });
  };

  const hasCreatePerm = hasPermission('SERVICE_CREATE_STANDARD');

  if (!currentCompanyId) {
    return (
      <Alert
        type="warning"
        message="Vui lòng chọn ngữ cảnh công ty trước."
        data-testid="no-company-warning"
      />
    );
  }

  if (!hasCreatePerm) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền tạo dịch vụ."
        data-testid="permission-denied"
      />
    );
  }

  const activeServiceTypes = serviceTypesData?.items.filter(st => st.isActive) || [];

  return (
    <div data-testid="service-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Tạo dịch vụ</Title>
        <Button onClick={() => navigate('/services')}>Hủy</Button>
      </Space>

      {formError && (
        <Alert
          type="error"
          message={formError}
          style={{ marginBottom: 16 }}
          data-testid="service-create-error"
        />
      )}

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        style={{ maxWidth: 600 }}
      >
        <Form.Item
          name="serviceTypeId"
          label="Loại dịch vụ"
          rules={[{ required: true, message: 'Vui lòng chọn loại dịch vụ' }]}
        >
          <Select
            showSearch
            placeholder="Chọn loại dịch vụ"
            optionFilterProp="children"
            data-testid="input-service-type"
          >
            {activeServiceTypes.map(st => (
              <Select.Option key={st.id} value={st.id}>
                {st.name} ({st.code}) - {st.standardPrice.toLocaleString()} {st.standardPriceCurrency}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>

        <Form.Item
          name="customerId"
          label="Khách hàng"
          rules={[{ required: true, message: 'Vui lòng chọn khách hàng' }]}
        >
          <RemoteSelect
            placeholder="Tìm theo tên hoặc mã khách hàng"
            queryKey={['service-create-customers']}
            fetchOptions={fetchCustomers}
            data-testid="input-customer-id"
          />
        </Form.Item>

        <Form.Item
          name="validFrom"
          label="Hiệu lực từ"
          rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu hiệu lực' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-valid-from" />
        </Form.Item>

        <Form.Item name="validTo" label="Hiệu lực đến (Tùy chọn)">
          <DatePicker style={{ width: '100%' }} data-testid="input-valid-to" />
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            loading={createMutation.isPending}
            data-testid="submit-service-btn"
          >
            Tạo
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
};

export default ServiceCreatePage;
