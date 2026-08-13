import React, { useEffect, useState } from 'react';
import {
  Alert, Button, Card, Col, Form, Input, InputNumber, Row, Select, Space, Spin, Tag, Typography,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getGraveById, updateGrave } from './gravesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import { GRAVE_STATUSES, GRAVE_TYPES, GRAVE_ZONES, graveTypeForCotCount } from './types';
import type { UpdateGraveRequest } from './types';
import { searchCustomers } from '../customers/customersApi';

const { Title } = Typography;
const { TextArea } = Input;

type OwnerOption = { label: string; value: number };

const GraveEditPage: React.FC = () => {
  const { graveId } = useParams<{ graveId: string }>();
  const id = Number(graveId);
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [ownerOptions, setOwnerOptions] = useState<OwnerOption[]>([]);
  const [ownerLoading, setOwnerLoading] = useState(false);
  const cotCountWatch = Form.useWatch('cotCount', form) as number | undefined;

  const { data, isLoading, error } = useQuery({
    queryKey: ['grave', id],
    queryFn: () => getGraveById(id),
    enabled: !Number.isNaN(id),
  });

  useEffect(() => {
    if (data) {
      form.setFieldsValue({
        zone: data.zone,
        plotNumber: data.plotNumber,
        rowLabel: data.rowLabel,
        colLabel: data.colLabel,
        areaM2: data.areaM2,
        cotCount: data.cotCount,
        status: data.status,
        ownerCustomerId: data.ownerCustomerId,
        notes: data.notes,
      });
      if (data.ownerCustomerId && data.ownerName) {
        setOwnerOptions([{ label: `${data.ownerName} (${data.ownerCode ?? ''})`, value: data.ownerCustomerId }]);
      }
    }
  }, [data, form]);

  const updateMutation = useMutation({
    mutationFn: (values: UpdateGraveRequest) => updateGrave(id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['graves'] });
      queryClient.invalidateQueries({ queryKey: ['grave', id] });
      navigate(`/graves/${id}`);
    },
    onError: (err) => setSubmitError(getErrorMessage(err)),
  });

  const handleOwnerSearch = async (text: string) => {
    if (!text || text.trim().length < 2) return;
    setOwnerLoading(true);
    try {
      const res = await searchCustomers({ search: text, page: 1, pageSize: 20 });
      setOwnerOptions(res.items.map((c) => ({ label: `${c.fullName} (${c.customerCode})`, value: c.id })));
    } catch {
      // ignore
    } finally {
      setOwnerLoading(false);
    }
  };

  const handleSubmit = (values: Record<string, unknown>) => {
    if (!data) return;
    setSubmitError(null);
    const request: UpdateGraveRequest = {
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
      targetVersion: data.rowVersion,
    };
    updateMutation.mutate(request);
  };

  if (isPermissionDenied(error)) return <Alert type="error" message="Bạn không có quyền sửa phần mộ." />;
  if (isLoading) return <Spin />;
  if (error || !data) return <Alert type="error" message={getErrorMessage(error)} />;

  return (
    <div data-testid="grave-edit-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Chỉnh sửa mộ {data.graveCode}</Title>
        <Button><Link to={`/graves/${id}`}>Quay lại</Link></Button>
      </Space>

      {submitError && (
        <Alert type="error" message={submitError} closable onClose={() => setSubmitError(null)} style={{ marginBottom: 16 }} />
      )}

      <Card>
        <Form form={form} layout="vertical" onFinish={handleSubmit} data-testid="grave-edit-form">
          <Row gutter={16}>
            <Col xs={12} sm={8}>
              <Form.Item name="zone" label="Khu" rules={[{ required: true }]}>
                <Select options={GRAVE_ZONES.map((z) => ({ label: `Khu ${z}`, value: z }))} />
              </Form.Item>
            </Col>
            <Col xs={12} sm={8}>
              <Form.Item name="plotNumber" label="Số mộ" rules={[{ required: true }, { max: 20 }]}><Input /></Form.Item>
            </Col>
            <Col xs={12} sm={8}>
              <Form.Item label="Loại mộ" tooltip="Tự xác định theo số cốt: 1 = Mộ đơn, 2 = Mộ đôi, ≥3 = Mộ gia tộc.">
                <span data-testid="derived-grave-type">
                  <Tag color="blue">{GRAVE_TYPES[graveTypeForCotCount(Number(cotCountWatch) || 1)]}</Tag>
                </span>
              </Form.Item>
            </Col>
            <Col xs={12} sm={6}><Form.Item name="rowLabel" label="Hàng"><Input /></Form.Item></Col>
            <Col xs={12} sm={6}><Form.Item name="colLabel" label="Cột"><Input /></Form.Item></Col>
            <Col xs={12} sm={6}>
              <Form.Item name="areaM2" label="Diện tích (m²)"><InputNumber min={0} step={0.1} style={{ width: '100%' }} /></Form.Item>
            </Col>
            <Col xs={12} sm={6}>
              <Form.Item name="cotCount" label="Số cốt" rules={[{ required: true, message: 'Nhập số cốt' }]}
                tooltip="Số cốt của mộ — dùng để khớp với gói chăm sóc">
                <InputNumber min={1} step={1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col xs={12} sm={6}>
              <Form.Item name="status" label="Trạng thái" rules={[{ required: true }]}>
                <Select options={Object.entries(GRAVE_STATUSES).map(([value, label]) => ({ label, value }))} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="ownerCustomerId" label="Chủ mộ (khách hàng)"
                tooltip="Đổi chủ mộ đã có nên dùng “Chuyển quyền sở hữu” ở trang chi tiết để lưu lịch sử.">
                <Select showSearch allowClear placeholder="Gõ tên/mã khách hàng để tìm..."
                  filterOption={false} onSearch={handleOwnerSearch} loading={ownerLoading} options={ownerOptions}
                  notFoundContent={ownerLoading ? 'Đang tìm...' : null} />
              </Form.Item>
            </Col>
            <Col xs={24}>
              <Form.Item name="notes" label="Ghi chú"><TextArea rows={2} /></Form.Item>
            </Col>
          </Row>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={updateMutation.isPending} data-testid="submit-edit">
                Lưu thay đổi
              </Button>
              <Button><Link to={`/graves/${id}`}>Hủy</Link></Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default GraveEditPage;
