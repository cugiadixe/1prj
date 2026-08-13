import React, { useState } from 'react';
import {
  Alert, Button, Card, Col, DatePicker, Divider, Form, Input, InputNumber, Row, Select, Space, Tag, Typography,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import type { Dayjs } from 'dayjs';
import { createGrave } from './gravesApi';
import { getErrorMessage } from './errorMessages';
import { GRAVE_STATUSES, GRAVE_TYPES, GRAVE_ZONES, graveTypeForCotCount } from './types';
import type { CreateGraveRequest } from './types';
import OccupantRelationshipFields from './OccupantRelationshipFields';
import { searchCustomers } from '../customers/customersApi';

const { Title } = Typography;
const { TextArea } = Input;

const d = (v: Dayjs | null | undefined) => (v ? v.format('YYYY-MM-DD') : null);

type OwnerOption = { label: string; value: number };

const GraveCreatePage: React.FC = () => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [ownerOptions, setOwnerOptions] = useState<OwnerOption[]>([]);
  const [ownerLoading, setOwnerLoading] = useState(false);
  const cotCountWatch = Form.useWatch('cotCount', form) as number | undefined;

  const createMutation = useMutation({
    mutationFn: (values: CreateGraveRequest) => createGrave(values),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['graves'] });
      navigate(`/graves/${result.id}`);
    },
    onError: (err) => setSubmitError(getErrorMessage(err)),
  });

  const handleOwnerSearch = async (text: string) => {
    if (!text || text.trim().length < 2) { setOwnerOptions([]); return; }
    setOwnerLoading(true);
    try {
      const res = await searchCustomers({ search: text, page: 1, pageSize: 20 });
      setOwnerOptions(res.items.map((c) => ({
        label: `${c.fullName} (${c.customerCode})`,
        value: c.id,
      })));
    } catch {
      setOwnerOptions([]);
    } finally {
      setOwnerLoading(false);
    }
  };

  const handleSubmit = (values: Record<string, unknown>) => {
    setSubmitError(null);
    const occupants = ((values.occupants as Record<string, unknown>[]) ?? []).map((o) => ({
      fullName: o.fullName as string,
      gender: (o.gender as string) || null,
      dob: d(o.dob as Dayjs),
      deathDateSolar: d(o.deathDateSolar as Dayjs),
      deathDateLunar: (o.deathDateLunar as string) || null,
      burialDate: d(o.burialDate as Dayjs),
      hometown: (o.hometown as string) || null,
      ownerRelationship: (o.ownerRelationship as string) || null,
      deceasedRelationship: (o.deceasedRelationship as string) || null,
      notes: (o.notes as string) || null,
    }));

    const request: CreateGraveRequest = {
      graveCode: values.graveCode as string,
      zone: values.zone as string,
      plotNumber: values.plotNumber as string,
      rowLabel: (values.rowLabel as string) || null,
      colLabel: (values.colLabel as string) || null,
      graveType: graveTypeForCotCount(values.cotCount != null ? Number(values.cotCount) : 1),
      areaM2: values.areaM2 != null ? Number(values.areaM2) : null,
      cotCount: values.cotCount != null ? Number(values.cotCount) : 1,
      status: values.status as string,
      ownerCustomerId: values.ownerCustomerId ? Number(values.ownerCustomerId) : null,
      notes: (values.notes as string) || null,
      occupants,
    };
    createMutation.mutate(request);
  };

  return (
    <div data-testid="grave-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Thêm mộ</Title>
        <Button><Link to="/graves">Quay lại danh sách</Link></Button>
      </Space>

      {submitError && (
        <Alert type="error" message={submitError} closable onClose={() => setSubmitError(null)}
          style={{ marginBottom: 16 }} data-testid="create-error" />
      )}

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          initialValues={{ status: 'EMPTY', cotCount: 1 }}
          data-testid="grave-create-form"
        >
          <Divider titlePlacement="left">Thông tin phần mộ</Divider>
          <Row gutter={16}>
            <Col xs={24} sm={8}>
              <Form.Item name="graveCode" label="Mã mộ"
                rules={[{ required: true, message: 'Mã mộ là bắt buộc' }, { max: 50, message: 'Tối đa 50 ký tự' }]}>
                <Input placeholder="VD: A-0001" data-testid="input-graveCode" />
              </Form.Item>
            </Col>
            <Col xs={12} sm={8}>
              <Form.Item name="zone" label="Khu" rules={[{ required: true, message: 'Chọn khu' }]}>
                <Select options={GRAVE_ZONES.map((z) => ({ label: `Khu ${z}`, value: z }))} data-testid="input-zone" />
              </Form.Item>
            </Col>
            <Col xs={12} sm={8}>
              <Form.Item name="plotNumber" label="Số mộ trong khu"
                rules={[{ required: true, message: 'Số mộ là bắt buộc' }, { max: 20 }]}>
                <Input placeholder="VD: 0001" data-testid="input-plotNumber" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={12} sm={6}><Form.Item name="rowLabel" label="Hàng"><Input /></Form.Item></Col>
            <Col xs={12} sm={6}><Form.Item name="colLabel" label="Cột"><Input /></Form.Item></Col>
            <Col xs={12} sm={6}>
              <Form.Item label="Loại mộ" tooltip="Tự xác định theo số cốt: 1 = Mộ đơn, 2 = Mộ đôi, ≥3 = Mộ gia tộc.">
                <span data-testid="derived-grave-type">
                  <Tag color="blue">{GRAVE_TYPES[graveTypeForCotCount(Number(cotCountWatch) || 1)]}</Tag>
                </span>
              </Form.Item>
            </Col>
            <Col xs={12} sm={6}>
              <Form.Item name="areaM2" label="Diện tích (m²)">
                <InputNumber min={0} step={0.1} style={{ width: '100%' }} data-testid="input-areaM2" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={12} sm={6}>
              <Form.Item name="cotCount" label="Số cốt" rules={[{ required: true, message: 'Nhập số cốt' }]}
                tooltip="Số cốt của mộ — dùng để khớp với gói chăm sóc">
                <InputNumber min={1} step={1} style={{ width: '100%' }} data-testid="input-cotCount" />
              </Form.Item>
            </Col>
            <Col xs={12} sm={6}>
              <Form.Item name="status" label="Trạng thái" rules={[{ required: true }]}>
                <Select options={Object.entries(GRAVE_STATUSES).map(([value, label]) => ({ label, value }))} data-testid="input-status" />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="notes" label="Ghi chú"><TextArea rows={1} /></Form.Item>
            </Col>
          </Row>

          <Divider titlePlacement="left">Chủ mộ & liên hệ khẩn cấp</Divider>
          <Row gutter={16}>
            <Col xs={24} sm={12}>
              <Form.Item name="ownerCustomerId" label="Chủ mộ (khách hàng)">
                <Select
                  showSearch
                  allowClear
                  placeholder="Gõ tên/mã khách hàng để tìm..."
                  filterOption={false}
                  onSearch={handleOwnerSearch}
                  loading={ownerLoading}
                  options={ownerOptions}
                  notFoundContent={ownerLoading ? 'Đang tìm...' : null}
                  data-testid="input-owner"
                />
              </Form.Item>
            </Col>
            <Col xs={24}>
              <Alert type="info" showIcon style={{ marginTop: 4 }}
                message="Liên hệ khẩn cấp (là khách hàng) được thêm ở trang chi tiết mộ sau khi tạo — có thể thêm nhiều người theo thứ tự ưu tiên." />
            </Col>
          </Row>

          <Divider titlePlacement="left">Người an táng</Divider>
          <Form.List name="occupants">
            {(fields, { add, remove }) => (
              <>
                {fields.map(({ key, name, ...rest }) => (
                  <Card key={key} size="small" style={{ marginBottom: 12 }}
                    title={`Người an táng #${name + 1}`}
                    extra={<Button type="text" danger icon={<MinusCircleOutlined />} onClick={() => remove(name)}>Xóa</Button>}>
                    <Row gutter={16}>
                      <Col xs={24} sm={8}>
                        <Form.Item {...rest} name={[name, 'fullName']} label="Họ tên"
                          rules={[{ required: true, message: 'Họ tên là bắt buộc' }]}>
                          <Input />
                        </Form.Item>
                      </Col>
                      <Col xs={12} sm={4}>
                        <Form.Item {...rest} name={[name, 'gender']} label="Giới tính">
                          <Select allowClear options={[
                            { label: 'Nam', value: 'MALE' },
                            { label: 'Nữ', value: 'FEMALE' },
                            { label: 'Khác', value: 'OTHER' },
                          ]} />
                        </Form.Item>
                      </Col>
                      <Col xs={12} sm={6}>
                        <Form.Item {...rest} name={[name, 'dob']} label="Ngày sinh">
                          <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
                        </Form.Item>
                      </Col>
                      <Col xs={12} sm={6}>
                        <Form.Item {...rest} name={[name, 'deathDateSolar']} label="Ngày mất (DL)">
                          <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
                        </Form.Item>
                      </Col>
                      <Col xs={12} sm={6}>
                        <Form.Item {...rest} name={[name, 'deathDateLunar']} label="Ngày mất (ÂL)" rules={[{ max: 20 }]}>
                          <Input placeholder="VD: 15/7 Giáp Thìn" />
                        </Form.Item>
                      </Col>
                      <Col xs={12} sm={6}>
                        <Form.Item {...rest} name={[name, 'burialDate']} label="Ngày an táng">
                          <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
                        </Form.Item>
                      </Col>
                      <Col xs={24} sm={12}>
                        <Form.Item {...rest} name={[name, 'hometown']} label="Nguyên quán"><Input /></Form.Item>
                      </Col>
                      <Col xs={24} sm={12}>
                        <OccupantRelationshipFields
                          form={form}
                          restField={rest}
                          ownerName={[name, 'ownerRelationship']}
                          deceasedName={[name, 'deceasedRelationship']}
                          genderName={[name, 'gender']}
                        />
                      </Col>
                    </Row>
                  </Card>
                ))}
                <Button type="dashed" onClick={() => add()} icon={<PlusOutlined />} block data-testid="add-occupant">
                  Thêm người an táng
                </Button>
              </>
            )}
          </Form.List>

          <Form.Item style={{ marginTop: 24 }}>
            <Space>
              <Button type="primary" htmlType="submit" loading={createMutation.isPending} data-testid="submit-create">
                Lưu mộ
              </Button>
              <Button><Link to="/graves">Hủy</Link></Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default GraveCreatePage;
