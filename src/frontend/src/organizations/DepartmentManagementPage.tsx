import React, { useState } from 'react';
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

const DepartmentManagementPage: React.FC = () => {
  const qc = useQueryClient();
  const [companyId, setCompanyId] = useState<number | undefined>(undefined);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<DepartmentDto | null>(null);
  const [form] = Form.useForm();

  const { data: companies } = useQuery({ queryKey: ['org-companies'], queryFn: listCompanies });
  const { data: departments, isLoading } = useQuery({
    queryKey: ['org-departments', companyId],
    queryFn: () => listDepartments(companyId!),
    enabled: !!companyId,
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['org-departments', companyId] });

  const saveMutation = useMutation({
    mutationFn: async (values: { departmentCode: string; name: string; parentDepartmentId?: number }) => {
      if (editing) {
        return updateDepartment(editing.id, { ...values, targetVersion: editing.rowVersion });
      }
      return createDepartment({ ...values, companyId: companyId! });
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

  const openCreate = () => { setEditing(null); form.resetFields(); setModalOpen(true); };
  const openEdit = (d: DepartmentDto) => {
    setEditing(d);
    form.setFieldsValue({ departmentCode: d.departmentCode, name: d.name, parentDepartmentId: d.parentDepartmentId });
    setModalOpen(true);
  };

  const columns = [
    { title: 'Mã', dataIndex: 'departmentCode', key: 'departmentCode' },
    { title: 'Tên phòng ban', dataIndex: 'name', key: 'name' },
    {
      title: 'Phòng cha', dataIndex: 'parentDepartmentId', key: 'parentDepartmentId',
      render: (pid: number | null) => pid ? (departments?.find((x) => x.id === pid)?.name ?? pid) : '—',
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
      <Title level={3}>Phòng ban</Title>
      <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
        <Select
          style={{ width: 320 }}
          showSearch
          optionFilterProp="label"
          placeholder="Chọn công ty để xem phòng ban"
          value={companyId}
          onChange={(v) => setCompanyId(v)}
          options={companies?.map((c) => ({ value: c.id, label: c.name }))}
          data-testid="dept-company-select"
        />
        <Button type="primary" icon={<PlusOutlined />} disabled={!companyId} onClick={openCreate} data-testid="create-department-btn">
          Thêm phòng ban
        </Button>
      </Space>

      {companyId ? (
        <Table rowKey="id" loading={isLoading} dataSource={departments} columns={columns} pagination={false} />
      ) : (
        <Typography.Text type="secondary">Chọn công ty để xem/quản lý phòng ban.</Typography.Text>
      )}

      <Modal
        title={editing ? 'Sửa phòng ban' : 'Thêm phòng ban'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditing(null); form.resetFields(); }}
        onOk={() => form.submit()}
        confirmLoading={saveMutation.isPending}
        okText="Lưu"
        cancelText="Hủy"
      >
        <Form form={form} layout="vertical" onFinish={(v) => saveMutation.mutate(v)}>
          <Form.Item name="departmentCode" label="Mã phòng ban" rules={[{ required: true, message: 'Nhập mã phòng ban' }]}>
            <Input placeholder="VD: HN-SALES" />
          </Form.Item>
          <Form.Item name="name" label="Tên phòng ban" rules={[{ required: true, message: 'Nhập tên phòng ban' }]}>
            <Input placeholder="VD: Phòng Kinh doanh" />
          </Form.Item>
          <Form.Item name="parentDepartmentId" label="Phòng cha (nếu có)">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="— Không —"
              options={departments?.filter((d) => !editing || d.id !== editing.id).map((d) => ({ value: d.id, label: d.name }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default DepartmentManagementPage;
