import React, { useState } from 'react';
import { Alert, Button, Card, DatePicker, Form, Input, Select, Space, Typography } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { createCustomer, checkDuplicates } from './customersApi';
import { getErrorMessage } from './errorMessages';
import type { CreateCustomerRequest, DuplicateCheckResult } from './types';

const { Title } = Typography;
const { TextArea } = Input;

const CustomerCreatePage: React.FC = () => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [duplicateWarning, setDuplicateWarning] = useState<DuplicateCheckResult | null>(null);

  const createMutation = useMutation({
    mutationFn: (values: CreateCustomerRequest) => createCustomer(values),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['customers'] });
      navigate(`/customers/${result.id}`);
    },
    onError: (err) => {
      setSubmitError(getErrorMessage(err));
    },
  });

  const handleDuplicateCheck = async (field: 'cccd' | 'phone') => {
    const value = form.getFieldValue(field);
    if (!value || value.trim() === '') {
      setDuplicateWarning(null);
      return;
    }
    try {
      const result = await checkDuplicates(
        field === 'cccd' ? { cccd: value } : { phone: value },
      );
      if (result.hasDuplicates) {
        setDuplicateWarning(result);
      } else {
        setDuplicateWarning(null);
      }
    } catch {
      // duplicate check is informational; ignore errors
    }
  };

  const handleSubmit = (values: Record<string, unknown>) => {
    setSubmitError(null);
    const request: CreateCustomerRequest = {
      customerCode: values.customerCode as string,
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
      initialCompanyId: values.initialCompanyId ? Number(values.initialCompanyId) : null,
      assignedStaffId: values.assignedStaffId ? Number(values.assignedStaffId) : null,
      internalNotes: (values.internalNotes as string) || null,
    };
    createMutation.mutate(request);
  };

  return (
    <div data-testid="customer-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Create Customer</Title>
        <Button>
          <Link to="/customers">Back to List</Link>
        </Button>
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

      {duplicateWarning && duplicateWarning.hasDuplicates && (
        <Alert
          type="warning"
          message="Possible duplicate customers found"
          description={
            <ul data-testid="duplicate-warning-list">
              {duplicateWarning.matches.map((m) => (
                <li key={m.id}>
                  {m.customerCode} — {m.fullName} (CCCD: {m.cccd ?? '—'})
                </li>
              ))}
            </ul>
          }
          closable
          onClose={() => setDuplicateWarning(null)}
          style={{ marginBottom: 16 }}
          data-testid="duplicate-warning"
        />
      )}

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          data-testid="customer-create-form"
        >
          <Form.Item
            name="customerCode"
            label="Customer Code"
            rules={[
              { required: true, message: 'Customer code is required' },
              { max: 50, message: 'Max 50 characters' },
            ]}
          >
            <Input data-testid="input-customerCode" />
          </Form.Item>

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
            <Input
              data-testid="input-cccd"
              onBlur={() => handleDuplicateCheck('cccd')}
            />
          </Form.Item>

          <Form.Item name="phone" label="Phone" rules={[{ max: 20, message: 'Max 20 characters' }]}>
            <Input
              data-testid="input-phone"
              onBlur={() => handleDuplicateCheck('phone')}
            />
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

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={createMutation.isPending}
                data-testid="submit-create"
              >
                Create Customer
              </Button>
              <Button>
                <Link to="/customers">Cancel</Link>
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default CustomerCreatePage;
