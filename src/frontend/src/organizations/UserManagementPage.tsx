import React, { useMemo, useState } from 'react';
import { Button, DatePicker, Form, Input, Modal, Select, Space, Table, Tag, Typography, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  listUsers, createUser, updateUser,
  listCompanies, listDepartments,
  type UserDto,
} from './organizationsApi';

const { Title } = Typography;

const EMPLOYMENT_OPTIONS = [
  { value: 'ACTIVE', label: 'Đang làm việc' },
  { value: 'INACTIVE', label: 'Nghỉ việc' },
];
const ACCOUNT_OPTIONS = [
  { value: 'ACTIVE', label: 'Hoạt động' },
  { value: 'LOCKED', label: 'Khoá' },
  { value: 'DISABLED', label: 'Vô hiệu' },
];

const STATUS_COLORS: Record<string, string> = { ACTIVE: 'green', INACTIVE: 'default', LOCKED: 'orange', DISABLED: 'red' };

const UserManagementPage: React.FC = () => {
  const qc = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<UserDto | null>(null);
  const [formCompanyId, setFormCompanyId] = useState<number | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [employmentFilter, setEmploymentFilter] = useState<string | undefined>(undefined);
  const [accountFilter, setAccountFilter] = useState<string | undefined>(undefined);
  const [form] = Form.useForm();

  const { data: users, isLoading } = useQuery({ queryKey: ['org-users'], queryFn: listUsers });

  const filteredUsers = useMemo(() => {
    let rows = users ?? [];
    if (employmentFilter) rows = rows.filter((u) => u.employmentStatus === employmentFilter);
    if (accountFilter) rows = rows.filter((u) => u.accountStatus === accountFilter);
    const q = search.trim().toLowerCase();
    if (q) rows = rows.filter((u) =>
      u.fullName.toLowerCase().includes(q) ||
      u.employeeCode.toLowerCase().includes(q) ||
      (u.email?.toLowerCase().includes(q) ?? false));
    return rows;
  }, [users, employmentFilter, accountFilter, search]);
  const { data: companies } = useQuery({ queryKey: ['org-companies'], queryFn: listCompanies });
  const { data: departments } = useQuery({
    queryKey: ['org-departments', formCompanyId],
    queryFn: () => listDepartments(formCompanyId!),
    enabled: !!formCompanyId && modalOpen && !editing,
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['org-users'] });

  const saveMutation = useMutation({
    mutationFn: async (values: {
      employeeCode: string; fullName: string; email?: string;
      employmentStatus: string; accountStatus: string;
      initialCompanyId?: number; initialDepartmentId?: number; effectiveFrom?: dayjs.Dayjs; reason?: string;
    }) => {
      if (editing) {
        return updateUser(editing.id, {
          employeeCode: values.employeeCode, fullName: values.fullName, email: values.email,
          employmentStatus: values.employmentStatus, accountStatus: values.accountStatus,
          targetVersion: editing.rowVersion,
        });
      }
      return createUser({
        employeeCode: values.employeeCode, fullName: values.fullName, email: values.email,
        employmentStatus: values.employmentStatus, accountStatus: values.accountStatus,
        initialCompanyId: values.initialCompanyId!, initialDepartmentId: values.initialDepartmentId!,
        effectiveFrom: (values.effectiveFrom ?? dayjs()).toISOString(), reason: values.reason,
      });
    },
    onSuccess: () => {
      message.success(editing ? 'Đã cập nhật người dùng' : 'Đã tạo người dùng');
      setModalOpen(false); setEditing(null); setFormCompanyId(undefined); form.resetFields(); invalidate();
    },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e?.response?.data?.detail || 'Lưu người dùng thất bại');
    },
  });

  const openCreate = () => {
    setEditing(null); setFormCompanyId(undefined); form.resetFields();
    form.setFieldsValue({ employmentStatus: 'ACTIVE', accountStatus: 'ACTIVE', effectiveFrom: dayjs() });
    setModalOpen(true);
  };
  const openEdit = (u: UserDto) => {
    setEditing(u); setFormCompanyId(undefined);
    form.setFieldsValue({
      employeeCode: u.employeeCode, fullName: u.fullName, email: u.email,
      employmentStatus: u.employmentStatus, accountStatus: u.accountStatus,
    });
    setModalOpen(true);
  };

  const columns = [
    { title: 'Mã NV', dataIndex: 'employeeCode', key: 'employeeCode' },
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    { title: 'Email', dataIndex: 'email', key: 'email', render: (v: string | null) => v || '—' },
    {
      title: 'Việc làm', dataIndex: 'employmentStatus', key: 'employmentStatus',
      render: (s: string) => <Tag color={STATUS_COLORS[s] ?? 'default'}>{s}</Tag>,
    },
    {
      title: 'Tài khoản', dataIndex: 'accountStatus', key: 'accountStatus',
      render: (s: string) => <Tag color={STATUS_COLORS[s] ?? 'default'}>{s}</Tag>,
    },
    {
      title: 'Thao tác', key: 'actions',
      render: (_: unknown, u: UserDto) => <Button size="small" onClick={() => openEdit(u)}>Sửa</Button>,
    },
  ];

  return (
    <div>
      <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={3} style={{ margin: 0 }}>Người dùng</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} data-testid="create-user-btn">Thêm người dùng</Button>
      </Space>

      <Space style={{ marginBottom: 16 }} wrap>
        <Input.Search
          allowClear style={{ width: 300 }} placeholder="Tìm theo mã NV / họ tên / email"
          onChange={(e) => setSearch(e.target.value)} data-testid="user-search"
        />
        <Select
          allowClear style={{ width: 180 }} placeholder="Trạng thái việc làm" value={employmentFilter}
          onChange={(v) => setEmploymentFilter(v)} options={EMPLOYMENT_OPTIONS}
        />
        <Select
          allowClear style={{ width: 180 }} placeholder="Trạng thái tài khoản" value={accountFilter}
          onChange={(v) => setAccountFilter(v)} options={ACCOUNT_OPTIONS}
        />
      </Space>

      <Table
        rowKey="id" loading={isLoading} dataSource={filteredUsers} columns={columns}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} người dùng` }}
      />

      <Modal
        title={editing ? 'Sửa người dùng' : 'Thêm người dùng'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditing(null); setFormCompanyId(undefined); form.resetFields(); }}
        onOk={() => form.submit()}
        confirmLoading={saveMutation.isPending}
        okText="Lưu"
        cancelText="Hủy"
        width={560}
      >
        <Form form={form} layout="vertical" onFinish={(v) => saveMutation.mutate(v)}>
          <Form.Item name="employeeCode" label="Mã nhân viên" rules={[{ required: true, message: 'Nhập mã nhân viên' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="fullName" label="Họ tên" rules={[{ required: true, message: 'Nhập họ tên' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="email" label="Email">
            <Input type="email" />
          </Form.Item>
          <Space style={{ width: '100%' }} size="middle">
            <Form.Item name="employmentStatus" label="Trạng thái việc làm" rules={[{ required: true }]} style={{ flex: 1, minWidth: 200 }}>
              <Select options={EMPLOYMENT_OPTIONS} />
            </Form.Item>
            <Form.Item name="accountStatus" label="Trạng thái tài khoản" rules={[{ required: true }]} style={{ flex: 1, minWidth: 200 }}>
              <Select options={ACCOUNT_OPTIONS} />
            </Form.Item>
          </Space>

          {!editing && (
            <>
              <Form.Item name="initialCompanyId" label="Công ty" rules={[{ required: true, message: 'Chọn công ty' }]}>
                <Select
                  showSearch optionFilterProp="label"
                  placeholder="Chọn công ty"
                  onChange={(v) => { setFormCompanyId(v); form.setFieldsValue({ initialDepartmentId: undefined }); }}
                  options={companies?.filter((c) => c.isActive).map((c) => ({ value: c.id, label: c.name }))}
                />
              </Form.Item>
              <Form.Item name="initialDepartmentId" label="Phòng ban" rules={[{ required: true, message: 'Chọn phòng ban' }]}>
                <Select
                  showSearch optionFilterProp="label"
                  disabled={!formCompanyId}
                  placeholder={formCompanyId ? 'Chọn phòng ban' : 'Chọn công ty trước'}
                  options={departments?.filter((d) => d.isActive).map((d) => ({ value: d.id, label: d.name }))}
                />
              </Form.Item>
              <Form.Item name="effectiveFrom" label="Hiệu lực từ" rules={[{ required: true }]}>
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>
    </div>
  );
};

export default UserManagementPage;
