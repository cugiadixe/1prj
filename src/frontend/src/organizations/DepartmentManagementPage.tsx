import React, { useMemo, useState } from 'react';
import { Button, Form, Input, Modal, Select, Space, Table, Tag, Typography, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  listCompanies,
  listDepartments,
  createDepartment,
  updateDepartment,
  setDepartmentStatus,
  type DepartmentDto,
} from './organizationsApi';

const { Title } = Typography;

type DeptRow = DepartmentDto & { companyName: string };

const DepartmentManagementPage: React.FC = () => {
  const qc = useQueryClient();
  const [companyFilter, setCompanyFilter] = useState<number | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | undefined>(undefined);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<DepartmentDto | null>(null);
  const [form] = Form.useForm();
  const formCompanyId = Form.useWatch('companyId', form) as number | undefined;

  const { data: companies } = useQuery({ queryKey: ['org-companies'], queryFn: listCompanies });

  // Gộp phòng ban của MỌI công ty (backend đòi companyId, gọi song song rồi ghép).
  const { data: allDepts, isLoading } = useQuery({
    queryKey: ['org-departments-all', companies?.map((c) => c.id).join(',')],
    enabled: !!companies && companies.length > 0,
    queryFn: async () => {
      const results = await Promise.all(
        (companies ?? []).map((c) =>
          listDepartments(c.id).then((ds) => ds.map((d): DeptRow => ({ ...d, companyName: c.name }))),
        ),
      );
      return results.flat();
    },
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['org-departments-all'] });

  const filtered = useMemo(() => {
    let rows = allDepts ?? [];
    if (companyFilter) rows = rows.filter((d) => d.companyId === companyFilter);
    if (statusFilter !== undefined) rows = rows.filter((d) => d.isActive === statusFilter);
    const q = search.trim().toLowerCase();
    if (q) rows = rows.filter((d) =>
      d.name.toLowerCase().includes(q) || d.departmentCode.toLowerCase().includes(q));
    return rows;
  }, [allDepts, companyFilter, statusFilter, search]);

  const saveMutation = useMutation({
    mutationFn: async (values: { departmentCode: string; name: string; companyId: number; parentDepartmentId?: number }) => {
      if (editing) {
        return updateDepartment(editing.id, {
          departmentCode: values.departmentCode, name: values.name,
          parentDepartmentId: values.parentDepartmentId, targetVersion: editing.rowVersion,
        });
      }
      return createDepartment(values);
    },
    onSuccess: () => {
      message.success(editing ? 'Đã cập nhật phòng ban' : 'Đã tạo phòng ban');
      setModalOpen(false); setEditing(null); form.resetFields(); invalidate();
    },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e?.response?.data?.detail || 'Lưu phòng ban thất bại');
    },
  });

  const statusMutation = useMutation({
    mutationFn: (d: DepartmentDto) => setDepartmentStatus(d.id, !d.isActive, d.rowVersion),
    onSuccess: () => { message.success('Đã đổi trạng thái'); invalidate(); },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e?.response?.data?.detail || 'Đổi trạng thái thất bại');
    },
  });

  const openCreate = () => {
    setEditing(null); form.resetFields();
    if (companyFilter) form.setFieldsValue({ companyId: companyFilter });
    setModalOpen(true);
  };
  const openEdit = (d: DepartmentDto) => {
    setEditing(d);
    form.setFieldsValue({ departmentCode: d.departmentCode, name: d.name, companyId: d.companyId, parentDepartmentId: d.parentDepartmentId });
    setModalOpen(true);
  };

  const parentOptions = useMemo(() => {
    const cid = editing ? editing.companyId : formCompanyId;
    return (allDepts ?? [])
      .filter((d) => d.companyId === cid && (!editing || d.id !== editing.id))
      .map((d) => ({ value: d.id, label: d.name }));
  }, [allDepts, editing, formCompanyId]);

  const columns = [
    { title: 'Công ty', dataIndex: 'companyName', key: 'companyName' },
    { title: 'Mã', dataIndex: 'departmentCode', key: 'departmentCode' },
    { title: 'Tên phòng ban', dataIndex: 'name', key: 'name' },
    {
      title: 'Phòng cha', dataIndex: 'parentDepartmentId', key: 'parentDepartmentId',
      render: (pid: number | null) => pid ? (allDepts?.find((x) => x.id === pid)?.name ?? pid) : '—',
    },
    {
      title: 'Trạng thái', dataIndex: 'isActive', key: 'isActive',
      render: (active: boolean) => <Tag color={active ? 'green' : 'red'}>{active ? 'Hoạt động' : 'Ngừng'}</Tag>,
    },
    {
      title: 'Thao tác', key: 'actions',
      render: (_: unknown, d: DepartmentDto) => (
        <Space>
          <Button size="small" onClick={() => openEdit(d)}>Sửa</Button>
          <Button size="small" danger={d.isActive} onClick={() => statusMutation.mutate(d)} loading={statusMutation.isPending}>
            {d.isActive ? 'Ngừng' : 'Kích hoạt'}
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }} wrap>
        <Title level={3} style={{ margin: 0 }}>Phòng ban</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} data-testid="create-department-btn">Thêm phòng ban</Button>
      </Space>

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear style={{ width: 260 }} showSearch optionFilterProp="label"
          placeholder="Lọc theo công ty" value={companyFilter} onChange={(v) => setCompanyFilter(v)}
          options={companies?.map((c) => ({ value: c.id, label: c.name }))}
          data-testid="dept-company-filter"
        />
        <Select
          allowClear style={{ width: 160 }} placeholder="Trạng thái" value={statusFilter}
          onChange={(v) => setStatusFilter(v)}
          options={[{ value: true, label: 'Hoạt động' }, { value: false, label: 'Ngừng' }]}
        />
        <Input.Search
          allowClear style={{ width: 280 }} placeholder="Tìm theo mã / tên phòng ban"
          onChange={(e) => setSearch(e.target.value)} data-testid="dept-search"
        />
      </Space>

      <Table
        rowKey="id" loading={isLoading} dataSource={filtered} columns={columns}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} phòng ban` }}
      />

      <Modal
        title={editing ? 'Sửa phòng ban' : 'Thêm phòng ban'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditing(null); form.resetFields(); }}
        onOk={() => form.submit()}
        confirmLoading={saveMutation.isPending}
        okText="Lưu" cancelText="Hủy"
      >
        <Form form={form} layout="vertical" onFinish={(v) => saveMutation.mutate(v)}>
          <Form.Item name="companyId" label="Công ty" rules={[{ required: true, message: 'Chọn công ty' }]}>
            <Select
              showSearch optionFilterProp="label" disabled={!!editing}
              placeholder="Chọn công ty"
              onChange={() => form.setFieldsValue({ parentDepartmentId: undefined })}
              options={companies?.map((c) => ({ value: c.id, label: c.name }))}
            />
          </Form.Item>
          <Form.Item name="departmentCode" label="Mã phòng ban" rules={[{ required: true, message: 'Nhập mã phòng ban' }]}>
            <Input placeholder="VD: HN-SALES" />
          </Form.Item>
          <Form.Item name="name" label="Tên phòng ban" rules={[{ required: true, message: 'Nhập tên phòng ban' }]}>
            <Input placeholder="VD: Phòng Kinh doanh" />
          </Form.Item>
          <Form.Item name="parentDepartmentId" label="Phòng cha (nếu có)">
            <Select allowClear showSearch optionFilterProp="label" placeholder="— Không —" options={parentOptions} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default DepartmentManagementPage;
