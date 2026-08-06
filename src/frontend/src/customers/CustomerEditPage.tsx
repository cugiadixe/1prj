import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, DatePicker, Form, Input, Select, Space, Spin, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getCustomerById, updateCustomer } from './customersApi';
import { getErrorMessage, isConcurrencyError, isPermissionDenied } from './errorMessages';
import type { UpdateCustomerRequest } from './types';

const { Title } = Typography;
const { TextArea } = Input;

const CustomerEditPage: React.FC = () => {
  const { customerId } = useParams<{ customerId: string }>();
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const id = Number(customerId);

  const [submitError, setSubmitError] = useState<string | null>(null);
  const [showConcurrencyRefresh, setShowConcurrencyRefresh] = useState(false);

  const {
    data: customer,
    isLoading,
    error: fetchError,
    refetch,
  } = useQuery({
    queryKey: ['customer', id],
    queryFn: () => getCustomerById(id),
    enabled: !isNaN(id),
  });

  useEffect(() => {
    if (customer) {
      const p = customer.profile;
      form.setFieldsValue({
        fullName: p.fullName,
        cccd: p.cccd,
        gender: p.gender,
        dobPartial: p.dobPartial,
        dobPrecision: p.dobPrecision,
        permanentAddress: p.permanentAddress,
        cccdIssuePlace: p.cccdIssuePlace,
        taxCode: p.taxCode,
        phone: p.phone,
        contactAddress: p.contactAddress,
        deathDateLunar: p.deathDateLunar,
        deathPlace: p.deathPlace,
        hometown: p.hometown,
      });
    }
  }, [customer, form]);

  const updateMutation = useMutation({
    mutationFn: (values: UpdateCustomerRequest) => updateCustomer(id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
      queryClient.invalidateQueries({ queryKey: ['customer', id] });
      navigate(`/customers/${id}`);
    },
    onError: (err) => {
      if (isConcurrencyError(err)) {
        setShowConcurrencyRefresh(true);
        setSubmitError(getErrorMessage(err));
      } else {
        setSubmitError(getErrorMessage(err));
      }
    },
  });

  const handleRefresh = async () => {
    setShowConcurrencyRefresh(false);
    setSubmitError(null);
    await refetch();
  };

  const handleSubmit = (values: Record<string, unknown>) => {
    if (!customer) return;
    setSubmitError(null);
    setShowConcurrencyRefresh(false);

    const request: UpdateCustomerRequest = {
      fullName: values.fullName as string,
      cccd: (values.cccd as string) || null,
      dob: values.dob ? (values.dob as { toISOString: () => string }).toISOString() : null,
      dobPartial: (values.dobPartial as string) || null,
      dobPrecision: (values.dobPrecision as string) || null,
      gender: (values.gender as string) || null,
      permanentAddress: (values.permanentAddress as string) || null,
      cccdIssueDate: values.cccdIssueDate ? (values.cccdIssueDate as { toISOString: () => string }).toISOString() : null,
      cccdIssuePlace: (values.cccdIssuePlace as string) || null,
      taxCode: (values.taxCode as string) || null,
      phone: (values.phone as string) || null,
      contactAddress: (values.contactAddress as string) || null,
      deathDateSolar: values.deathDateSolar ? (values.deathDateSolar as { toISOString: () => string }).toISOString() : null,
      deathDateLunar: (values.deathDateLunar as string) || null,
      deathPlace: (values.deathPlace as string) || null,
      hometown: (values.hometown as string) || null,
      reason: values.reason as string,
      targetVersion: customer.rowVersion,
    };
    updateMutation.mutate(request);
  };

  if (isLoading) return <Spin data-testid="customer-edit-loading" />;

  if (isPermissionDenied(fetchError)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to edit this customer."
        data-testid="permission-denied"
      />
    );
  }

  if (fetchError) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(fetchError)}
        data-testid="customer-edit-fetch-error"
      />
    );
  }

  if (!customer) return null;

  return (
    <div data-testid="customer-edit-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Edit Customer: {customer.customerCode}
        </Title>
        <Button>
          <Link to={`/customers/${id}`}>Back to Detail</Link>
        </Button>
      </Space>

      {submitError && (
        <Alert
          type="error"
          message={submitError}
          closable={!showConcurrencyRefresh}
          onClose={() => setSubmitError(null)}
          style={{ marginBottom: 16 }}
          data-testid="edit-error"
          action={
            showConcurrencyRefresh ? (
              <Button size="small" type="primary" onClick={handleRefresh} data-testid="refresh-btn">
                Refresh
              </Button>
            ) : undefined
          }
        />
      )}

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          data-testid="customer-edit-form"
        >
          <Form.Item
            name="fullName"
            label="Full Name"
            rules={[
              { required: true, message: 'Full name is required' },
              { max: 200, message: 'Max 200 characters' },
            ]}
          >
            <Input data-testid="input-fullName" />
          </Form.Item>

          <Form.Item name="cccd" label="CCCD" rules={[{ max: 20, message: 'Max 20 characters' }]}>
            <Input data-testid="input-cccd" />
          </Form.Item>

          <Form.Item name="phone" label="Phone" rules={[{ max: 20, message: 'Max 20 characters' }]}>
            <Input data-testid="input-phone" />
          </Form.Item>

          <Form.Item name="gender" label="Gender">
            <Select
              allowClear
              data-testid="input-gender"
              options={[
                { label: 'Male', value: 'MALE' },
                { label: 'Female', value: 'FEMALE' },
                { label: 'Other', value: 'OTHER' },
              ]}
            />
          </Form.Item>

          <Form.Item name="dob" label="Date of Birth">
            <DatePicker style={{ width: '100%' }} data-testid="input-dob" />
          </Form.Item>

          <Form.Item name="dobPartial" label="DOB Partial" rules={[{ max: 10, message: 'Max 10 characters' }]}>
            <Input data-testid="input-dobPartial" />
          </Form.Item>

          <Form.Item name="dobPrecision" label="DOB Precision">
            <Select
              allowClear
              data-testid="input-dobPrecision"
              options={[
                { label: 'Full', value: 'FULL' },
                { label: 'Year & Month', value: 'YEAR_MONTH' },
                { label: 'Year', value: 'YEAR' },
                { label: 'Unknown', value: 'UNKNOWN' },
              ]}
            />
          </Form.Item>

          <Form.Item name="permanentAddress" label="Permanent Address" rules={[{ max: 500, message: 'Max 500 characters' }]}>
            <TextArea rows={2} data-testid="input-permanentAddress" />
          </Form.Item>

          <Form.Item name="cccdIssueDate" label="CCCD Issue Date">
            <DatePicker style={{ width: '100%' }} data-testid="input-cccdIssueDate" />
          </Form.Item>

          <Form.Item name="cccdIssuePlace" label="CCCD Issue Place" rules={[{ max: 200, message: 'Max 200 characters' }]}>
            <Input data-testid="input-cccdIssuePlace" />
          </Form.Item>

          <Form.Item name="taxCode" label="Tax Code" rules={[{ max: 20, message: 'Max 20 characters' }]}>
            <Input data-testid="input-taxCode" />
          </Form.Item>

          <Form.Item name="contactAddress" label="Contact Address" rules={[{ max: 500, message: 'Max 500 characters' }]}>
            <TextArea rows={2} data-testid="input-contactAddress" />
          </Form.Item>

          <Form.Item name="deathDateSolar" label="Death Date (Solar)">
            <DatePicker style={{ width: '100%' }} data-testid="input-deathDateSolar" />
          </Form.Item>

          <Form.Item name="deathDateLunar" label="Death Date (Lunar)" rules={[{ max: 20, message: 'Max 20 characters' }]}>
            <Input data-testid="input-deathDateLunar" />
          </Form.Item>

          <Form.Item name="deathPlace" label="Death Place" rules={[{ max: 200, message: 'Max 200 characters' }]}>
            <Input data-testid="input-deathPlace" />
          </Form.Item>

          <Form.Item name="hometown" label="Hometown" rules={[{ max: 200, message: 'Max 200 characters' }]}>
            <Input data-testid="input-hometown" />
          </Form.Item>

          <Form.Item
            name="reason"
            label="Reason for Update"
            rules={[
              { required: true, message: 'Reason is required' },
              { max: 500, message: 'Max 500 characters' },
            ]}
          >
            <TextArea rows={2} data-testid="input-reason" />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={updateMutation.isPending}
                data-testid="submit-update"
              >
                Update Customer
              </Button>
              <Button>
                <Link to={`/customers/${id}`}>Cancel</Link>
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default CustomerEditPage;
