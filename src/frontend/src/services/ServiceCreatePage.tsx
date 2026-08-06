import React, { useState } from 'react';
import { Alert, Button, DatePicker, Form, InputNumber, Space, Typography, message, Select } from 'antd';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { createService } from './servicesApi';
import { searchServiceTypes } from './serviceTypesApi';
import { getErrorMessage } from './errorMessages';
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
      message.success('Service created successfully');
      navigate(`/services/${data.id}`);
    },
    onError: (err) => {
      setFormError(getErrorMessage(err));
    },
  });

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

  const hasCreatePerm = hasPermission('SERVICE_CREATE_STANDARD', 'COMPANY');

  if (!currentCompanyId) {
    return (
      <Alert
        type="warning"
        message="Please select a company context first."
        data-testid="no-company-warning"
      />
    );
  }

  if (!hasCreatePerm) {
    return (
      <Alert
        type="error"
        message="You do not have permission to create services."
        data-testid="permission-denied"
      />
    );
  }

  const activeServiceTypes = serviceTypesData?.items.filter(st => st.isActive) || [];

  return (
    <div data-testid="service-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Create Service</Title>
        <Button onClick={() => navigate('/services')}>Cancel</Button>
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
          label="Service Type"
          rules={[{ required: true, message: 'Please select a service type' }]}
        >
          <Select
            showSearch
            placeholder="Select a service type"
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
          label="Customer ID"
          rules={[{ required: true, message: 'Please enter a customer ID' }]}
        >
          <InputNumber
            style={{ width: '100%' }}
            min={1}
            data-testid="input-customer-id"
          />
        </Form.Item>

        <Form.Item
          name="validFrom"
          label="Valid From"
          rules={[{ required: true, message: 'Please select a valid from date' }]}
        >
          <DatePicker style={{ width: '100%' }} data-testid="input-valid-from" />
        </Form.Item>

        <Form.Item name="validTo" label="Valid To (Optional)">
          <DatePicker style={{ width: '100%' }} data-testid="input-valid-to" />
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            loading={createMutation.isPending}
            data-testid="submit-service-btn"
          >
            Create
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
};

export default ServiceCreatePage;
