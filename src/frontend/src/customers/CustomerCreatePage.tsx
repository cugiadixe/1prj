import React, { useState } from 'react';
import { Alert, Button, Card, Col, DatePicker, Form, Input, Row, Select, Space, Tabs, Typography } from 'antd';
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
  const [activeTab, setActiveTab] = useState('basic');
  // Loại giấy tờ tuỳ thân: CCCD = 12 số, CMND = 10 số. Chỉ điều khiển kiểm tra ở FE;
  // số giấy tờ vẫn lưu vào trường cccd (loại suy theo độ dài).
  const docType = (Form.useWatch('docType', form) as string) || 'CCCD';
  const idDigits = docType === 'CMND' ? 10 : 12;

  // Chỉ giữ chữ số khi gõ (dùng cho số giấy tờ, điện thoại, mã số thuế).
  const digitsOnly = (e: React.ChangeEvent<HTMLInputElement>) => e.target.value.replace(/\D/g, '');

  // Trường bắt buộc (customerCode, fullName) đều ở tab "basic"; nếu submit lỗi thì nhảy về đó.
  const onFinishFailed = () => {
    setActiveTab('basic');
  };

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
        <Title level={4} style={{ margin: 0 }}>Tạo khách hàng</Title>
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
          onFinishFailed={onFinishFailed}
          initialValues={{ docType: 'CCCD' }}
          data-testid="customer-create-form"
        >
          <Tabs
            activeKey={activeTab}
            onChange={setActiveTab}
            items={[
              {
                key: 'basic',
                label: 'Thông tin cơ bản',
                forceRender: true,
                children: (
                  <Row gutter={16}>
                    <Col xs={24} md={12}>
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
                    </Col>
                    <Col xs={24} md={12}>
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
                    </Col>
                    <Col xs={24} md={12}>
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
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item name="dob" label="Ngày sinh">
                        <DatePicker style={{ width: '100%' }} data-testid="input-dob" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item name="dobPartial" label="Ngày sinh (một phần)" rules={[{ max: 10, message: 'Tối đa 10 ký tự' }]}>
                        <Input data-testid="input-dobPartial" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
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
                    </Col>
                  </Row>
                ),
              },
              {
                key: 'contact',
                label: 'Giấy tờ & liên hệ',
                forceRender: true,
                children: (
                  <Row gutter={16}>
                    <Col xs={24} md={12}>
                      <Form.Item name="docType" label="Loại giấy tờ">
                        <Select
                          data-testid="input-docType"
                          options={[
                            { label: 'CCCD (12 số)', value: 'CCCD' },
                            { label: 'CMND (10 số)', value: 'CMND' },
                          ]}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item
                        name="cccd"
                        label={`Số ${docType === 'CMND' ? 'CMND' : 'CCCD'}`}
                        dependencies={['docType']}
                        getValueFromEvent={digitsOnly}
                        rules={[
                          {
                            validator: (_, v) => {
                              if (!v) return Promise.resolve();
                              if (!/^\d+$/.test(v)) return Promise.reject(new Error('Chỉ gồm chữ số'));
                              if (v.length !== idDigits) {
                                return Promise.reject(new Error(`${docType === 'CMND' ? 'CMND' : 'CCCD'} phải gồm đúng ${idDigits} chữ số`));
                              }
                              return Promise.resolve();
                            },
                          },
                        ]}
                      >
                        <Input
                          data-testid="input-cccd"
                          inputMode="numeric"
                          maxLength={idDigits}
                          placeholder={`${idDigits} chữ số`}
                          onBlur={() => handleDuplicateCheck('cccd')}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item
                        name="phone"
                        label="Điện thoại"
                        getValueFromEvent={digitsOnly}
                        rules={[
                          {
                            validator: (_, v) =>
                              !v || /^\d{10}$/.test(v)
                                ? Promise.resolve()
                                : Promise.reject(new Error('Điện thoại phải gồm đúng 10 chữ số')),
                          },
                        ]}
                      >
                        <Input
                          data-testid="input-phone"
                          inputMode="numeric"
                          maxLength={10}
                          placeholder="10 chữ số"
                          onBlur={() => handleDuplicateCheck('phone')}
                        />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item name="cccdIssueDate" label="Ngày cấp CCCD">
                        <DatePicker style={{ width: '100%' }} data-testid="input-cccdIssueDate" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item name="cccdIssuePlace" label="Nơi cấp CCCD" rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}>
                        <Input data-testid="input-cccdIssuePlace" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item
                        name="taxCode"
                        label="Mã số thuế"
                        getValueFromEvent={digitsOnly}
                        rules={[
                          {
                            validator: (_, v) =>
                              !v || /^\d+$/.test(v)
                                ? Promise.resolve()
                                : Promise.reject(new Error('Mã số thuế chỉ gồm chữ số')),
                          },
                          { max: 20, message: 'Tối đa 20 ký tự' },
                        ]}
                      >
                        <Input data-testid="input-taxCode" inputMode="numeric" placeholder="Chỉ chữ số" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item name="hometown" label="Quê quán" rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}>
                        <Input data-testid="input-hometown" />
                      </Form.Item>
                    </Col>
                    <Col xs={24}>
                      <Form.Item name="permanentAddress" label="Địa chỉ thường trú" rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}>
                        <TextArea rows={2} data-testid="input-permanentAddress" />
                      </Form.Item>
                    </Col>
                    <Col xs={24}>
                      <Form.Item name="contactAddress" label="Địa chỉ liên hệ" rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}>
                        <TextArea rows={2} data-testid="input-contactAddress" />
                      </Form.Item>
                    </Col>
                  </Row>
                ),
              },
              {
                key: 'death',
                label: 'Thông tin mất',
                forceRender: true,
                children: (
                  <Row gutter={16}>
                    <Col xs={24} md={12}>
                      <Form.Item name="deathDateSolar" label="Ngày mất (Dương lịch)">
                        <DatePicker style={{ width: '100%' }} data-testid="input-deathDateSolar" />
                      </Form.Item>
                    </Col>
                    <Col xs={24} md={12}>
                      <Form.Item name="deathDateLunar" label="Ngày mất (Âm lịch)" rules={[{ max: 20, message: 'Tối đa 20 ký tự' }]}>
                        <Input data-testid="input-deathDateLunar" />
                      </Form.Item>
                    </Col>
                    <Col xs={24}>
                      <Form.Item name="deathPlace" label="Nơi mất" rules={[{ max: 200, message: 'Tối đa 200 ký tự' }]}>
                        <Input data-testid="input-deathPlace" />
                      </Form.Item>
                    </Col>
                  </Row>
                ),
              },
            ]}
          />

          <Form.Item style={{ marginTop: 8 }}>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                loading={createMutation.isPending}
                data-testid="submit-create"
              >
                Tạo khách hàng
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

export default CustomerCreatePage;
