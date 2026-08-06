import React, { useEffect, useState } from 'react';
import { Alert, Button, Form, Input, InputNumber, Space, Spin, Typography, message } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { createServiceType, getServiceTypeById, updateServiceType } from './serviceTypesApi';
import { getErrorMessage, isPermissionDenied, isConcurrencyError } from './errorMessages';
import type { CreateServiceTypeRequest, UpdateServiceTypeRequest } from './types';

const { Title } = Typography;
const { TextArea } = Input;

const ServiceTypeFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const isEdit = Boolean(id);
  const serviceTypeId = parseInt(id || '0', 10);

  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();

  const [formError, setFormError] = useState<string | null>(null);

  const { data: initialData, isLoading: isLoadingInitial, error: initialError } = useQuery({
    queryKey: ['serviceType', serviceTypeId],
    queryFn: () => getServiceTypeById(serviceTypeId),
    enabled: isEdit,
  });

  useEffect(() => {
    if (initialData && isEdit) {
      form.setFieldsValue({
        name: initialData.name,
        description: initialData.description,
        cycleDurationMonths: initialData.cycleDurationMonths,
      });
    }
  }, [initialData, isEdit, form]);

  const createMutation = useMutation({
    mutationFn: (values: CreateServiceTypeRequest) => createServiceType(values),
    onSuccess: (data) => {
      message.success('Service type created successfully');
      navigate(`/services/types/${data.id}`);
    },
    onError: (err) => {
      setFormError(getErrorMessage(err));
    },
  });

  const updateMutation = useMutation({
    mutationFn: (values: UpdateServiceTypeRequest) => updateServiceType(serviceTypeId, values),
    onSuccess: (data) => {
      message.success('Service type updated successfully');
      queryClient.setQueryData(['serviceType', serviceTypeId], data);
      navigate(`/services/types/${data.id}`);
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        setFormError('This record was modified by another user. Please refresh and try again.');
      } else {
        setFormError(getErrorMessage(err));
      }
    },
  });

  const handleSubmit = (values: any) => {
    setFormError(null);
    if (isEdit) {
      if (!initialData) return;
      updateMutation.mutate({
        name: values.name,
        description: values.description,
        cycleDurationMonths: values.cycleDurationMonths,
        rowVersion: initialData.rowVersion,
      });
    } else {
      createMutation.mutate({
        code: values.code,
        name: values.name,
        description: values.description,
        standardPrice: values.standardPrice,
        cycleDurationMonths: values.cycleDurationMonths,
      });
    }
  };

  const hasManagePerm = hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL');

  if (!hasManagePerm || isPermissionDenied(initialError)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to manage service types."
        data-testid="permission-denied"
      />
    );
  }

  if (isEdit && initialError) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(initialError)}
        data-testid="service-type-form-initial-error"
      />
    );
  }

  if (isEdit && isLoadingInitial) {
    return <Spin data-testid="service-type-form-loading" />;
  }

  return (
    <div data-testid="service-type-form-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          {isEdit ? 'Edit Service Type' : 'Create Service Type'}
        </Title>
        <Button onClick={() => navigate(isEdit ? `/services/types/${serviceTypeId}` : '/services/types')}>
          Cancel
        </Button>
      </Space>

      {formError && (
        <Alert
          type="error"
          message={formError}
          style={{ marginBottom: 16 }}
          data-testid="service-type-form-error"
          action={
            isEdit && isConcurrencyError(formError) ? (
              <Button size="small" type="primary" onClick={() => queryClient.invalidateQueries({ queryKey: ['serviceType', serviceTypeId] })}>
                Refresh
              </Button>
            ) : undefined
          }
        />
      )}

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        style={{ maxWidth: 600 }}
      >
        {!isEdit && (
          <Form.Item
            name="code"
            label="Code"
            rules={[{ required: true, message: 'Please enter a code' }]}
          >
            <Input data-testid="input-code" />
          </Form.Item>
        )}

        <Form.Item
          name="name"
          label="Name"
          rules={[{ required: true, message: 'Please enter a name' }]}
        >
          <Input data-testid="input-name" />
        </Form.Item>

        <Form.Item name="description" label="Description">
          <TextArea rows={4} data-testid="input-description" />
        </Form.Item>

        {!isEdit && (
          <Form.Item
            name="standardPrice"
            label="Standard Price"
            rules={[{ required: true, message: 'Please enter a standard price' }]}
          >
            <InputNumber
              style={{ width: '100%' }}
              min={0}
              step={1000}
              data-testid="input-standard-price"
            />
          </Form.Item>
        )}

        <Form.Item name="cycleDurationMonths" label="Cycle Duration (Months)">
          <InputNumber
            style={{ width: '100%' }}
            min={1}
            max={120}
            data-testid="input-cycle-duration"
          />
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            loading={createMutation.isPending || updateMutation.isPending}
            data-testid="submit-service-type-btn"
          >
            {isEdit ? 'Save Changes' : 'Create'}
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
};

export default ServiceTypeFormPage;
