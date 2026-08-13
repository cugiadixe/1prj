import React, { useState } from 'react';
import { Alert, Button, Card, DatePicker, Form, Input, Select, Space, Typography } from 'antd';
import dayjs from 'dayjs';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { createCustomerMasterChangeRequest } from './customerMasterChangeApi';
import { getErrorMessage } from './errorMessages';
import type { CreateCustomerMasterChangeRequest } from './customerMasterChangeTypes';
import type { ProfileInfo } from './types';

const { Title } = Typography;
const { TextArea } = Input;

interface CustomerMasterChangeRequestFormProps {
  customerId: number;
  customerName: string;
  targetRowVersion: string;
  profile: ProfileInfo;
  onCancel: () => void;
}

/** Chuỗi rỗng/khoảng trắng coi như không có giá trị, để so sánh delta. */
const norm = (v: unknown): string => (v == null ? '' : String(v).trim());

const CustomerMasterChangeRequestForm: React.FC<CustomerMasterChangeRequestFormProps> = ({
  customerId,
  customerName,
  targetRowVersion,
  profile,
  onCancel,
}) => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Đổ sẵn thông tin hiện tại của khách để anh nhìn và sửa trực tiếp.
  const initialValues = {
    fullName: profile.fullName ?? undefined,
    cccd: profile.cccd ?? undefined,
    phone: profile.phone ?? undefined,
    gender: profile.gender ?? undefined,
    dob: profile.dob ? dayjs(profile.dob) : undefined,
    dobPartial: profile.dobPartial ?? undefined,
    dobPrecision: profile.dobPrecision ?? undefined,
    permanentAddress: profile.permanentAddress ?? undefined,
    cccdIssueDate: profile.cccdIssueDate ? dayjs(profile.cccdIssueDate) : undefined,
    cccdIssuePlace: profile.cccdIssuePlace ?? undefined,
    taxCode: profile.taxCode ?? undefined,
    contactAddress: profile.contactAddress ?? undefined,
    deathDateSolar: profile.deathDateSolar ? dayjs(profile.deathDateSolar) : undefined,
    deathDateLunar: profile.deathDateLunar ?? undefined,
    deathPlace: profile.deathPlace ?? undefined,
    hometown: profile.hometown ?? undefined,
  };

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

    // Chỉ gửi trường THỰC SỰ thay đổi so với giá trị gốc (form đã đổ sẵn giá trị hiện tại).
    const txt = (key: keyof ProfileInfo, val: unknown): string | null =>
      norm(val) === norm(profile[key]) ? null : ((val as string) || null);
    const dt = (key: keyof ProfileInfo, val: unknown): string | null => {
      const orig = profile[key] as string | null;
      const next = val ? (val as dayjs.Dayjs).toISOString() : null;
      if (!orig && !next) return null;
      if (orig && next && dayjs(orig).isSame(dayjs(next), 'day')) return null;
      return next;
    };

    const request: CreateCustomerMasterChangeRequest = {
      targetCustomerId: customerId,
      targetRowVersion,
      fullName: txt('fullName', values.fullName),
      cccd: txt('cccd', values.cccd),
      dob: dt('dob', values.dob),
      dobPartial: txt('dobPartial', values.dobPartial),
      dobPrecision: txt('dobPrecision', values.dobPrecision),
      gender: txt('gender', values.gender),
      permanentAddress: txt('permanentAddress', values.permanentAddress),
      cccdIssueDate: dt('cccdIssueDate', values.cccdIssueDate),
      cccdIssuePlace: txt('cccdIssuePlace', values.cccdIssuePlace),
      taxCode: txt('taxCode', values.taxCode),
      phone: txt('phone', values.phone),
      contactAddress: txt('contactAddress', values.contactAddress),
      deathDateSolar: dt('deathDateSolar', values.deathDateSolar),
      deathDateLunar: txt('deathDateLunar', values.deathDateLunar),
      deathPlace: txt('deathPlace', values.deathPlace),
      hometown: txt('hometown', values.hometown),
      reason: values.reason as string,
    };
    createMutation.mutate(request);
  };

  return (
    <div data-testid="customer-master-change-request-form">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Yêu cầu thay đổi cho khách hàng: {customerName}
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
          initialValues={initialValues}
          onFinish={handleSubmit}
          data-testid="customer-master-change-form"
        >
          <Form.Item
            name="reason"
            label="Lý do thay đổi"
            rules={[
              { required: true, message: 'Lý do là bắt buộc' },
              { max: 500, message: 'Tối đa 500 ký tự' },
            ]}
          >
            <TextArea rows={2} data-testid="input-reason" />
          </Form.Item>

          <Alert
            message="Thông tin hiện tại đã được điền sẵn. Chỉ cần sửa vào các trường bạn muốn thay đổi — hệ thống chỉ ghi nhận những trường thực sự khác so với hiện tại."
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <Form.Item
            name="fullName"
            label="Họ tên"
            rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}
          >
            <Input data-testid="input-fullName" />
          </Form.Item>

          <Form.Item name="cccd" label="CCCD" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
            <Input data-testid="input-cccd" />
          </Form.Item>

          <Form.Item name="phone" label="Điện thoại" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
            <Input data-testid="input-phone" />
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

          <Form.Item
            name="dobPartial"
            label="Ngày sinh (một phần)"
            rules={[{ max: 10, message: 'Tối đa 10 ký tự' }]}
          >
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

          <Form.Item
            name="permanentAddress"
            label="Địa chỉ thường trú"
            rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}
          >
            <TextArea rows={2} data-testid="input-permanentAddress" />
          </Form.Item>

          <Form.Item name="cccdIssueDate" label="Ngày cấp CCCD">
            <DatePicker style={{ width: '100%' }} data-testid="input-cccdIssueDate" />
          </Form.Item>

          <Form.Item
            name="cccdIssuePlace"
            label="Nơi cấp CCCD"
            rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}
          >
            <Input data-testid="input-cccdIssuePlace" />
          </Form.Item>

          <Form.Item
            name="taxCode"
            label="Mã số thuế"
            rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}
          >
            <Input data-testid="input-taxCode" />
          </Form.Item>

          <Form.Item
            name="contactAddress"
            label="Địa chỉ liên hệ"
            rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}
          >
            <TextArea rows={2} data-testid="input-contactAddress" />
          </Form.Item>

          <Form.Item name="deathDateSolar" label="Ngày mất (Dương lịch)">
            <DatePicker style={{ width: '100%' }} data-testid="input-deathDateSolar" />
          </Form.Item>

          <Form.Item
            name="deathDateLunar"
            label="Ngày mất (Âm lịch)"
            rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}
          >
            <Input data-testid="input-deathDateLunar" />
          </Form.Item>

          <Form.Item
            name="deathPlace"
            label="Nơi mất"
            rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}
          >
            <Input data-testid="input-deathPlace" />
          </Form.Item>

          <Form.Item
            name="hometown"
            label="Quê quán"
            rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}
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
                Gửi yêu cầu thay đổi
              </Button>
              <Button onClick={onCancel}>Hủy</Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default CustomerMasterChangeRequestForm;
