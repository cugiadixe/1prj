import React, { useState } from 'react';
import {
  Alert, Button, Card, DatePicker, Form, Input, Modal, Popconfirm,
  Select, Space, Switch, Table, Tag, Typography, message,
} from 'antd';
import { PlusOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import {
  closeAuthority, createAuthority, getApprovalAuthorityErrorMessage,
  listApproverOptions, listAuthorities, listCompanies, listDepartments,
} from './api';
import type { ApprovalAuthority, CreateApprovalAuthorityRequest } from './types';
import { AA_STATUS_COLORS, AA_STATUS_LABELS, AUTHORITY_LEVEL_LABELS } from './types';

const { Title, Text } = Typography;

const money = (v: number | null) => (v == null ? '—' : v.toLocaleString('vi-VN') + ' đ');
const day = (v: string | null) => (v ? dayjs(v).format('DD/MM/YYYY') : '—');

const ApprovalAuthorityPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [modalOpen, setModalOpen] = useState(false);
  const [filterCompanyId, setFilterCompanyId] = useState<number | undefined>();
  const [includeClosed, setIncludeClosed] = useState(false);

  const formCompanyId = Form.useWatch('companyId', form);
  const formDepartmentId = Form.useWatch('departmentId', form);

  const { data: companies } = useQuery({
    queryKey: ['org-companies'],
    queryFn: listCompanies,
  });

  const { data: filterDepartments } = useQuery({
    queryKey: ['org-departments', filterCompanyId],
    queryFn: () => listDepartments(filterCompanyId),
    enabled: filterCompanyId != null,
  });

  const { data: formDepartments } = useQuery({
    queryKey: ['org-departments', formCompanyId],
    queryFn: () => listDepartments(formCompanyId),
    enabled: modalOpen && formCompanyId != null,
  });

  // Danh sách người duyệt để chọn theo tên — lọc theo công ty (và phòng ban nếu đã chọn).
  const { data: approverOptions, isFetching: approverLoading } = useQuery({
    queryKey: ['approver-options', formCompanyId, formDepartmentId],
    queryFn: () => listApproverOptions(formCompanyId, formDepartmentId),
    enabled: modalOpen && formCompanyId != null,
  });
  const approverSelectOptions = (approverOptions ?? []).map((u) => ({
    value: u.id,
    label: `${u.fullName} (${u.employeeCode})`,
  }));

  const { data: authorities, isLoading } = useQuery({
    queryKey: ['approval-authorities', filterCompanyId, includeClosed],
    queryFn: () => listAuthorities({ companyId: filterCompanyId, includeClosed }),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['approval-authorities'] });

  const createMutation = useMutation({
    mutationFn: (req: CreateApprovalAuthorityRequest) => createAuthority(req),
    onSuccess: () => {
      message.success('Đã thêm dòng thẩm quyền phê duyệt');
      setModalOpen(false);
      form.resetFields();
      invalidate();
    },
    onError: (err: unknown) => message.error(getApprovalAuthorityErrorMessage(err)),
  });

  const closeMutation = useMutation({
    mutationFn: (id: number) => closeAuthority(id, dayjs().toISOString()),
    onSuccess: () => {
      message.success('Đã đóng dòng thẩm quyền');
      invalidate();
    },
    onError: (err: unknown) => message.error(getApprovalAuthorityErrorMessage(err)),
  });

  const openCreate = () => {
    form.resetFields();
    form.setFieldsValue({ authorityLevel: 1, effectiveFrom: dayjs() });
    setModalOpen(true);
  };

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      const req: CreateApprovalAuthorityRequest = {
        companyId: values.companyId,
        departmentId: values.departmentId,
        processCode: values.processCode?.trim() ? values.processCode.trim() : null,
        approverUserId: values.approverUserId,
        authorityLevel: values.authorityLevel,
        minAmount: values.minAmount ?? null,
        maxAmount: values.maxAmount ?? null,
        effectiveFrom: (values.effectiveFrom as dayjs.Dayjs).toISOString(),
        effectiveTo: values.effectiveTo ? (values.effectiveTo as dayjs.Dayjs).toISOString() : null,
        delegatedFromUserId: values.delegatedFromUserId ?? null,
        notes: values.notes?.trim() ? values.notes.trim() : null,
      };
      createMutation.mutate(req);
    });
  };

  const columns = [
    { title: 'Công ty', dataIndex: 'companyName', key: 'companyName', render: (v: string | null) => v ?? '—' },
    { title: 'Phòng ban', dataIndex: 'departmentName', key: 'departmentName', render: (v: string | null) => v ?? '—' },
    {
      title: 'Cấp', dataIndex: 'authorityLevel', key: 'authorityLevel',
      render: (v: number) => AUTHORITY_LEVEL_LABELS[v] ?? `Cấp ${v}`,
    },
    {
      title: 'Người duyệt', key: 'approver',
      render: (_: unknown, r: ApprovalAuthority) => (
        <span>
          {r.approverName ?? `Người dùng ${r.approverUserId}`}
          {r.delegatedFromUserId != null && (
            <Tag color="purple" style={{ marginInlineStart: 8 }}>
              Uỷ quyền thay {r.delegatedFromName ?? `#${r.delegatedFromUserId}`}
            </Tag>
          )}
        </span>
      ),
    },
    {
      title: 'Quy trình', dataIndex: 'processCode', key: 'processCode',
      render: (v: string | null) => v ?? <Text type="secondary">Mọi quy trình</Text>,
    },
    {
      title: 'Ngưỡng tiền', key: 'amount',
      render: (_: unknown, r: ApprovalAuthority) =>
        r.minAmount == null && r.maxAmount == null
          ? <Text type="secondary">Không giới hạn</Text>
          : `${money(r.minAmount)} – ${money(r.maxAmount)}`,
    },
    {
      title: 'Hiệu lực', key: 'effective',
      render: (_: unknown, r: ApprovalAuthority) => `${day(r.effectiveFrom)} → ${day(r.effectiveTo)}`,
    },
    {
      title: 'Trạng thái', dataIndex: 'status', key: 'status',
      render: (v: string) => <Tag color={AA_STATUS_COLORS[v] ?? 'default'}>{AA_STATUS_LABELS[v] ?? v}</Tag>,
    },
    {
      title: 'Thao tác', key: 'action',
      render: (_: unknown, r: ApprovalAuthority) => (
        r.status === 'ACTIVE' ? (
          <Popconfirm
            title="Đóng dòng thẩm quyền này?"
            description="Từ nay dòng này hết hiệu lực. Dùng khi thu hồi hoặc thay người duyệt."
            okText="Đóng" cancelText="Huỷ"
            onConfirm={() => closeMutation.mutate(r.id)}
          >
            <Button danger size="small">Đóng</Button>
          </Popconfirm>
        ) : null
      ),
    },
  ];

  return (
    <Card>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Space align="center" style={{ justifyContent: 'space-between', width: '100%' }}>
          <Title level={4} style={{ margin: 0 }}>
            <SafetyCertificateOutlined style={{ marginInlineEnd: 8 }} />
            Thẩm quyền phê duyệt
          </Title>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Thêm thẩm quyền
          </Button>
        </Space>

        <Alert
          type="info"
          showIcon
          message="Ô này quyết định ai được duyệt"
          description="Mỗi dòng khai một người duyệt cho một (công ty, phòng ban, cấp). Cấp 1 = Trưởng phòng, cấp 2 = Giám đốc. Nghỉ phép: đóng dòng cũ rồi thêm dòng uỷ quyền cho người thay."
        />

        <Space wrap>
          <Select
            allowClear
            placeholder="Lọc theo công ty"
            style={{ minWidth: 220 }}
            value={filterCompanyId}
            onChange={(v) => setFilterCompanyId(v)}
            options={(companies ?? []).map((c) => ({ value: c.id, label: c.name }))}
          />
          {filterDepartments && filterDepartments.length > 0 && (
            <Text type="secondary">{filterDepartments.length} phòng ban trong công ty này</Text>
          )}
          <Space>
            <Switch checked={includeClosed} onChange={setIncludeClosed} />
            <Text>Hiện cả dòng đã đóng</Text>
          </Space>
        </Space>

        <Table
          rowKey="id"
          loading={isLoading}
          dataSource={authorities ?? []}
          columns={columns}
          size="small"
          pagination={{ pageSize: 20, hideOnSinglePage: true }}
        />
      </Space>

      <Modal
        title="Thêm dòng thẩm quyền phê duyệt"
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={createMutation.isPending}
        okText="Lưu"
        cancelText="Huỷ"
        width={640}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="companyId" label="Công ty" rules={[{ required: true, message: 'Chọn công ty' }]}>
            <Select
              placeholder="Chọn công ty"
              options={(companies ?? []).map((c) => ({ value: c.id, label: c.name }))}
              onChange={() => form.setFieldValue('departmentId', undefined)}
            />
          </Form.Item>

          <Form.Item name="departmentId" label="Phòng ban" rules={[{ required: true, message: 'Chọn phòng ban' }]}>
            <Select
              placeholder={formCompanyId ? 'Chọn phòng ban' : 'Chọn công ty trước'}
              disabled={!formCompanyId}
              options={(formDepartments ?? []).map((d) => ({ value: d.id, label: d.name }))}
            />
          </Form.Item>

          <Form.Item name="authorityLevel" label="Cấp thẩm quyền" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 1, label: 'Cấp 1 — Trưởng phòng' },
                { value: 2, label: 'Cấp 2 — Giám đốc' },
              ]}
            />
          </Form.Item>

          <Form.Item
            name="approverUserId"
            label="Người duyệt"
            tooltip="Tìm theo tên hoặc mã nhân viên. Danh sách lọc theo công ty (và phòng ban nếu đã chọn)."
            rules={[{ required: true, message: 'Chọn người duyệt' }]}
          >
            <Select
              showSearch
              placeholder={formCompanyId ? 'Tìm theo tên hoặc mã nhân viên' : 'Chọn công ty trước'}
              disabled={!formCompanyId}
              loading={approverLoading}
              options={approverSelectOptions}
              optionFilterProp="label"
              notFoundContent={approverLoading ? 'Đang tải...' : 'Không có người phù hợp trong công ty/phòng đã chọn'}
            />
          </Form.Item>

          <Form.Item
            name="processCode"
            label="Mã quy trình"
            tooltip="Để trống = áp cho mọi quy trình."
          >
            <Input placeholder="Để trống = mọi quy trình (ví dụ: ASSIGN_CARE_PACKAGE)" />
          </Form.Item>

          {/* Ngưỡng tiền tạm ẩn ở pilot: resolver chưa lọc theo số tiền hồ sơ nên điền vào không có
              tác dụng. Cột hiển thị sẽ luôn là "Không giới hạn". Bật lại khi nối đa cấp theo tiền (D7). */}

          <Space size="large" style={{ display: 'flex' }}>
            <Form.Item
              name="effectiveFrom" label="Hiệu lực từ" style={{ flex: 1 }}
              rules={[{ required: true, message: 'Chọn ngày bắt đầu' }]}
            >
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
            <Form.Item name="effectiveTo" label="Hiệu lực đến (để trống = vô hạn)" style={{ flex: 1 }}>
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
          </Space>

          <Form.Item
            name="delegatedFromUserId"
            label="Uỷ quyền thay ai — chỉ khi là dòng uỷ quyền nghỉ phép"
            tooltip="Nếu chọn, dòng này thay hẳn người được uỷ quyền trong thời gian hiệu lực."
          >
            <Select
              allowClear
              showSearch
              placeholder="Để trống nếu là thẩm quyền thường"
              disabled={!formCompanyId}
              loading={approverLoading}
              options={approverSelectOptions}
              optionFilterProp="label"
            />
          </Form.Item>

          <Form.Item name="notes" label="Ghi chú">
            <Input.TextArea rows={2} maxLength={2000} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default ApprovalAuthorityPage;
