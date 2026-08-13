import React, { useMemo, useState } from 'react';
import {
  Alert, Button, Card, DatePicker, Form, InputNumber, Modal, Popconfirm, Select, Space, Table, Tag, Typography, message,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { usePermissions } from '../auth/AuthProvider';
import { assignGrave, cancelPackage, createPackage, getCcpErrorMessage, listByCustomer } from './api';
import { CCP_STATUS_COLORS, CCP_STATUS_LABELS, type CustomerCarePackage } from './types';
import { searchServiceTypes } from '../services/serviceTypesApi';
import { searchGraves } from '../graves/gravesApi';

const { Text } = Typography;
const fmtMoney = (v: number) => v.toLocaleString('vi-VN') + ' đ';
const fmtDate = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');

interface Props {
  customerId: number;
}

const CustomerCarePackagesSection: React.FC<Props> = ({ customerId }) => {
  const { hasPermission } = usePermissions();
  const canManage = hasPermission('CUSTOMER_CARE_PACKAGE_MANAGE', 'GLOBAL');
  const canView = hasPermission('CUSTOMER_CARE_PACKAGE_VIEW', 'GLOBAL');
  const queryClient = useQueryClient();

  const [assignForm] = Form.useForm();
  const [addOpen, setAddOpen] = useState(false);
  const [assignTarget, setAssignTarget] = useState<CustomerCarePackage | null>(null);

  const { data: packages, isLoading } = useQuery({
    queryKey: ['ccp', customerId],
    queryFn: () => listByCustomer(customerId),
    enabled: !Number.isNaN(customerId) && canView,
  });

  // Danh mục gói chăm sóc (lọc is_care_package)
  const { data: serviceTypesRes } = useQuery({
    queryKey: ['care-package-types'],
    queryFn: () => searchServiceTypes({ page: 1, pageSize: 100 }),
    enabled: addOpen,
  });
  const carePackageOptions = useMemo(
    () => (serviceTypesRes?.items ?? [])
      .filter((s) => s.isCarePackage && s.isActive)
      .map((s) => ({
        value: s.id,
        label: `${s.name} — ${fmtMoney(s.standardPrice)}${s.cycleDurationMonths ? `/${s.cycleDurationMonths} tháng` : ''}`,
      })),
    [serviceTypesRes],
  );

  // Mộ của khách (để gán) — lọc theo số cốt của gói
  const { data: gravesRes, isLoading: gravesLoading } = useQuery({
    queryKey: ['owner-graves', customerId],
    queryFn: () => searchGraves({ ownerCustomerId: customerId, pageSize: 100 }),
    enabled: assignTarget != null,
  });
  const matchingGraveOptions = useMemo(() => {
    if (!assignTarget) return [];
    return (gravesRes?.items ?? [])
      .filter((g) => g.cotCount === assignTarget.cotCount)
      .map((g) => ({ value: g.id, label: `${g.graveCode} — Khu ${g.zone}, ${g.cotCount} cốt` }));
  }, [gravesRes, assignTarget]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ccp', customerId] });
    queryClient.invalidateQueries({ queryKey: ['owner-graves', customerId] });
  };

  const createMut = useMutation({
    mutationFn: (v: Record<string, unknown>) => createPackage({
      customerId,
      serviceTypeId: Number(v.serviceTypeId),
      cotCount: Number(v.cotCount),
      startDate: (v.startDate as dayjs.Dayjs).format('YYYY-MM-DD'),
      notes: (v.notes as string) || null,
    }),
    onSuccess: () => { message.success('Đã gán gói cho khách'); setAddOpen(false); invalidate(); },
    onError: (e) => message.error(getCcpErrorMessage(e)),
  });

  const assignMut = useMutation({
    mutationFn: (graveId: number) => assignGrave(assignTarget!.id, graveId),
    onSuccess: () => { message.success('Đã gán gói vào mộ'); setAssignTarget(null); assignForm.resetFields(); invalidate(); },
    onError: (e) => message.error(getCcpErrorMessage(e)),
  });

  const cancelMut = useMutation({
    mutationFn: (id: number) => cancelPackage(id),
    onSuccess: () => { message.success('Đã hủy gói'); invalidate(); },
    onError: (e) => message.error(getCcpErrorMessage(e)),
  });

  if (!canView) return null;

  const columns = [
    { title: 'Gói chăm sóc', dataIndex: 'serviceTypeName', key: 'serviceTypeName', render: (v: string | null) => v ?? '—' },
    { title: 'Số cốt', dataIndex: 'cotCount', key: 'cotCount' },
    {
      title: 'Kỳ hạn',
      key: 'period',
      render: (_: unknown, r: CustomerCarePackage) => `${fmtDate(r.startDate)} → ${r.endDate ? fmtDate(r.endDate) : 'Không kỳ hạn'}`,
    },
    { title: 'Thành tiền', dataIndex: 'totalPrice', key: 'totalPrice', render: (v: number) => fmtMoney(v) },
    {
      title: 'Mộ đã gán',
      key: 'grave',
      render: (_: unknown, r: CustomerCarePackage) =>
        r.graveId ? <Tag color="blue">{r.graveCode}</Tag> : <Text type="secondary">Chưa gán</Text>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (s: string) => <Tag color={CCP_STATUS_COLORS[s] ?? 'default'}>{CCP_STATUS_LABELS[s] ?? s}</Tag>,
    },
    ...(canManage ? [{
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, r: CustomerCarePackage) => (
        <Space>
          {r.status !== 'CANCELLED' && (
            <Button size="small" type="link" onClick={() => setAssignTarget(r)} data-testid={`assign-grave-${r.id}`}>
              {r.graveId ? 'Đổi mộ' : 'Gán vào mộ'}
            </Button>
          )}
          {r.status !== 'CANCELLED' && (
            <Popconfirm title="Hủy gói này?" okText="Hủy gói" cancelText="Không" onConfirm={() => cancelMut.mutate(r.id)}>
              <Button size="small" type="link" danger>Hủy</Button>
            </Popconfirm>
          )}
        </Space>
      ),
    }] : []),
  ];

  return (
    <Card
      title="Gói chăm sóc của khách"
      style={{ marginBottom: 16 }}
      data-testid="customer-care-packages-card"
      extra={canManage && (
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setAddOpen(true)} data-testid="add-ccp-btn">
          Gán gói
        </Button>
      )}
    >
      {isLoading && <Table loading columns={columns} dataSource={[]} rowKey="id" pagination={false} />}
      {!isLoading && packages && packages.length === 0 && (
        <Alert type="info" message="Khách hàng chưa có gói chăm sóc nào." />
      )}
      {!isLoading && packages && packages.length > 0 && (
        <Table columns={columns} dataSource={packages} rowKey="id" pagination={false} size="small" />
      )}

      {/* Modal: Gán gói cho khách */}
      <Modal
        title="Gán gói chăm sóc cho khách"
        open={addOpen}
        onCancel={() => setAddOpen(false)}
        okText="Lưu"
        cancelText="Hủy"
        okButtonProps={{ form: 'ccp-add-form', htmlType: 'submit', loading: createMut.isPending }}
        destroyOnHidden
      >
        <Form id="ccp-add-form" layout="vertical" onFinish={(v) => createMut.mutate(v)}
          initialValues={{ cotCount: 1, startDate: dayjs() }}>
          <Form.Item name="serviceTypeId" label="Gói chăm sóc" rules={[{ required: true, message: 'Chọn gói' }]}>
            <Select options={carePackageOptions} placeholder="Chọn gói chăm sóc"
              notFoundContent="Chưa có loại dịch vụ nào được đánh dấu 'Là gói chăm sóc'" />
          </Form.Item>
          <Form.Item name="cotCount" label="Số cốt" rules={[{ required: true, message: 'Nhập số cốt' }]}
            tooltip="Số cốt của gói — sẽ phải khớp với số cốt của mộ khi gán">
            <InputNumber min={1} step={1} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="startDate" label="Ngày bắt đầu" rules={[{ required: true, message: 'Chọn ngày bắt đầu' }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal: Gán vào mộ */}
      <Modal
        title={`Gán gói vào mộ${assignTarget ? ` (${assignTarget.cotCount} cốt)` : ''}`}
        open={assignTarget != null}
        onCancel={() => { setAssignTarget(null); assignForm.resetFields(); }}
        okText="Gán"
        cancelText="Hủy"
        okButtonProps={{ form: 'ccp-assign-form', htmlType: 'submit', loading: assignMut.isPending }}
        destroyOnHidden
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Chỉ hiển thị mộ thuộc sở hữu của khách và có số cốt khớp với gói."
        />
        <Form id="ccp-assign-form" form={assignForm} layout="vertical"
          onFinish={(v) => assignMut.mutate(Number(v.graveId))}>
          <Form.Item name="graveId" label="Phần mộ" rules={[{ required: true, message: 'Chọn mộ' }]}>
            <Select
              options={matchingGraveOptions}
              loading={gravesLoading}
              placeholder="Chọn mộ khớp số cốt"
              notFoundContent={gravesLoading ? 'Đang tải...' : `Không có mộ ${assignTarget?.cotCount ?? ''} cốt thuộc khách này`}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default CustomerCarePackagesSection;
