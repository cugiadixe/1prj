import React, { useState } from 'react';
import { Alert, Button, Card, DatePicker, Form, Input, Select, Space, Typography } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { createCustomerProposal } from './customerProposalApi';
import { checkDuplicates } from './customersApi';
import { getErrorMessage } from './errorMessages';
import type { CreateCustomerProposalRequest } from './customerProposalTypes';
import type { DuplicateCheckResult } from './types';

const { Title } = Typography;
const { TextArea } = Input;

const CustomerProposalCreatePage: React.FC = () => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [duplicateWarning, setDuplicateWarning] = useState<DuplicateCheckResult | null>(null);

  const createMutation = useMutation({
    mutationFn: (values: CreateCustomerProposalRequest) => createCustomerProposal(values),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['customer-proposals'] });
      navigate(`/customers/proposals/${result.id}`);
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
    const request: CreateCustomerProposalRequest = {
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
    <div data-testid="customer-proposal-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Gửi đề xuất tạo khách hàng</Title>
        <Button>
          <Link to="/customers">Quay lại danh sách</Link>
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
          message="Phát hiện khách hàng có thể trùng lặp"
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
          data-testid="customer-proposal-create-form"
        >
          <Form.Item
            name="customerCode"
            label="Mã khách hàng"
            rules={[
              { required: true, message: 'Mã khách hàng là bắt buộc' },
              { max: 50, message: 'Tối đa 50 ký tự' },
            ]}
          >
            <Input data-testid="input-customerCode" />
          </Form.Item>

          <Form.Item
            name="fullName"
            label="Họ tên"
            rules={[
              { required: true, message: 'Họ tên là bắt buộc' },
              { max: 200, message: 'Tối đa 200 ký tự' },
            ]}
          >
            <Input data-testid="input-fullName" />
          </Form.Item>

          <Form.Item name="cccd" label="CCCD" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
            <Input
              data-testid="input-cccd"
              onBlur={() => handleDuplicateCheck('cccd')}
            />
          </Form.Item>

          <Form.Item name="phone" label="Điện thoại" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
            <Input
              data-testid="input-phone"
              onBlur={() => handleDuplicateCheck('phone')}
            />
          </Form.Item>

          <Form.Item name="gender" label="Giới tính">
            <Select
              allowClear
              data-testid="input-gender"
              options={[
                { label: 'Nam', value: 'MALE' },
                { label: 'Nữ', value: 'FEMALE' },
                { label: 'Khác', value: 'OTHER' },
              ]}
            />
          </Form.Item>

          <Form.Item name="dob" label="Ngày sinh">
            <DatePicker style={{ width: '100%' }} data-testid="input-dob" />
          </Form.Item>

          <Form.Item name="dobPartial" label="Ngày sinh (một phần)" rules={[{ max: 10, message: 'Tối đa 10 ký tự' }]}>
            <Input data-testid="input-dobPartial" />
          </Form.Item>

          <Form.Item name="dobPrecision" label="Độ chính xác ngày sinh">
            <Select
              allowClear
              data-testid="input-dobPrecision"
              options={[
                { label: 'Đầy đủ', value: 'FULL' },
                { label: 'Năm & Tháng', value: 'YEAR_MONTH' },
                { label: 'Năm', value: 'YEAR' },
                { label: 'Không rõ', value: 'UNKNOWN' },
              ]}
            />
          </Form.Item>

          <Form.Item name="permanentAddress" label="Địa chỉ thường trú" rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}>
            <TextArea rows={2} data-testid="input-permanentAddress" />
          </Form.Item>

          <Form.Item name="cccdIssueDate" label="Ngày cấp CCCD">
            <DatePicker style={{ width: '100%' }} data-testid="input-cccdIssueDate" />
          </Form.Item>

          <Form.Item name="cccdIssuePlace" label="Nơi cấp CCCD" rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}>
            <Input data-testid="input-cccdIssuePlace" />
          </Form.Item>

          <Form.Item name="taxCode" label="Mã số thuế" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
            <Input data-testid="input-taxCode" />
          </Form.Item>

          <Form.Item name="contactAddress" label="Địa chỉ liên hệ" rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}>
            <TextArea rows={2} data-testid="input-contactAddress" />
          </Form.Item>

          <Form.Item name="deathDateSolar" label="Ngày mất (Dương lịch)">
            <DatePicker style={{ width: '100%' }} data-testid="input-deathDateSolar" />
          </Form.Item>

          <Form.Item name="deathDateLunar" label="Ngày mất (Âm lịch)" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
            <Input data-testid="input-deathDateLunar" />
          </Form.Item>

          <Form.Item name="deathPlace" label="Nơi mất" rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}>
            <Input data-testid="input-deathPlace" />
          </Form.Item>

          <Form.Item name="hometown" label="Quê quán" rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}>
            <Input data-testid="input-hometown" />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={createMutation.isPending}
                data-testid="submit-create-proposal"
              >
                Gửi đề xuất
              </Button>
              <Button>
                <Link to="/customers">Hủy</Link>
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default CustomerProposalCreatePage;
