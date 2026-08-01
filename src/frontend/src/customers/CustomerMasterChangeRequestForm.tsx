import React, { useState } from 'react';
import { Alert, Button, Card, DatePicker, Form, Input, Select, Space, Typography } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { createCustomerMasterChangeRequest } from './customerMasterChangeApi';
import { getErrorMessage } from './errorMessages';
import type { CreateCustomerMasterChangeRequest } from './customerMasterChangeTypes';

const { Title } = Typography;
const { TextArea } = Input;

interface CustomerMasterChangeRequestFormProps {
  customerId: number;
  customerName: string;
  targetRowVersion: string;
  onCancel: () => void;
}

const CustomerMasterChangeRequestForm: React.FC<CustomerMasterChangeRequestFormProps> = ({
  customerId,
  customerName,
  targetRowVersion,
  onCancel,
}) => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (values: CreateCustomerMasterChangeRequest) =>
      createCustomerMasterChangeRequest(customerId, values),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['my-change-requests'] });
      navigate(`/customers/change-requests/${result.id}`);
    },
    onError: (err) => {
      setSubmitError(getErrorMessage(err));
    },
  });

  const handleSubmit = (values: Record<string, unknown>) => {
    setSubmitError(null);
    const request: CreateCustomerMasterChangeRequest = {
      targetCustomerId: customerId,
      targetRowVersion,
      fullName: (values.fullName as string) || null,
      cccd: (values.cccd as string) || null,
      dob: values.dob ? (values.dob as { toISOString: () => string }).toISOString() : null,
      dobPartial: (values.dobPartial as string) || null,
      dobPrecision: (values.dobPrecision as string) || null,
      gender: (values.gender as string) || null,
      permanentAddress: (values.permanentAddress as string) || null,
      cccdIssueDate: values.cccdIssueDate
        ? (values.cccdIssueDate as { toISOString: () => string }).toISOString()
        : null,
      cccdIssuePlace: (values.cccdIssuePlace as string) || null,
      taxCode: (values.taxCode as string) || null,
      phone: (values.phone as string) || null,
      contactAddress: (values.contactAddress as string) || null,
      deathDateSolar: values.deathDateSolar
        ? (values.deathDateSolar as { toISOString: () => string }).toISOString()
        : null,
      deathDateLunar: (values.deathDateLunar as string) || null,
      deathPlace: (values.deathPlace as string) || null,
      hometown: (values.hometown as string) || null,
      reason: values.reason as string,
    };
    createMutation.mutate(request);
  };

  return (
    <div data-testid="customer-master-change-request-form">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Request Change for Customer: {customerName}
        </Title>
      </Space>

      {submitError && (
        <Alert
          type="error"
          message={submitError}
          closable
          onClose={() => setSubmitError(null)}
          style={{ marginBottom: 16 }}
          data-testid="create-error"
        />
      )}

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          data-testid="customer-master-change-form"
        >
          <Form.Item
            name="reason"
            label="Reason for Change"
            rules={[
              { required: true, message: 'Reason is required' },
              { max: 500, message: 'Max 500 characters' },
            ]}
          >
            <TextArea rows={2} data-testid="input-reason" />
          </Form.Item>

          <Alert
            message="Only fill in the fields you wish to change."
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <Form.Item
            name="fullName"
            label="Full Name"
            rules={[{ max: 200, message: 'Max 200 characters' }]}
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

          <Form.Item
            name="dobPartial"
            label="DOB Partial"
            rules={[{ max: 10, message: 'Max 10 characters' }]}
          >
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

          <Form.Item
            name="permanentAddress"
            label="Permanent Address"
            rules={[{ max: 500, message: 'Max 500 characters' }]}
          >
            <TextArea rows={2} data-testid="input-permanentAddress" />
          </Form.Item>

          <Form.Item name="cccdIssueDate" label="CCCD Issue Date">
            <DatePicker style={{ width: '100%' }} data-testid="input-cccdIssueDate" />
          </Form.Item>

          <Form.Item
            name="cccdIssuePlace"
            label="CCCD Issue Place"
            rules={[{ max: 200, message: 'Max 200 characters' }]}
          >
            <Input data-testid="input-cccdIssuePlace" />
          </Form.Item>

          <Form.Item
            name="taxCode"
            label="Tax Code"
            rules={[{ max: 20, message: 'Max 20 characters' }]}
          >
            <Input data-testid="input-taxCode" />
          </Form.Item>

          <Form.Item
            name="contactAddress"
            label="Contact Address"
            rules={[{ max: 500, message: 'Max 500 characters' }]}
          >
            <TextArea rows={2} data-testid="input-contactAddress" />
          </Form.Item>

          <Form.Item name="deathDateSolar" label="Death Date (Solar)">
            <DatePicker style={{ width: '100%' }} data-testid="input-deathDateSolar" />
          </Form.Item>

          <Form.Item
            name="deathDateLunar"
            label="Death Date (Lunar)"
            rules={[{ max: 20, message: 'Max 20 characters' }]}
          >
            <Input data-testid="input-deathDateLunar" />
          </Form.Item>

          <Form.Item
            name="deathPlace"
            label="Death Place"
            rules={[{ max: 200, message: 'Max 200 characters' }]}
          >
            <Input data-testid="input-deathPlace" />
          </Form.Item>

          <Form.Item
            name="hometown"
            label="Hometown"
            rules={[{ max: 200, message: 'Max 200 characters' }]}
          >
            <Input data-testid="input-hometown" />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={createMutation.isPending}
                data-testid="submit-change-request"
              >
                Submit Change Request
              </Button>
              <Button onClick={onCancel}>Cancel</Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default CustomerMasterChangeRequestForm;
