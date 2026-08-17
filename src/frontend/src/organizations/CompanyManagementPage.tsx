import React, { useState } from 'react';
import { Button, Form, Input, Modal, Select, Space, Table, Tag, Typography, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  listCompanies,
  createCompany,
  updateCompany,
  setCompanyStatus,
  type CompanyDto,
} from './organizationsApi';

const { Title } = Typography;

const CompanyManagementPage: React.FC = () => {
  const qc = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CompanyDto | null>(null);
  const [form] = Form.useForm();

  const { data: companies, isLoading } = useQuery({ queryKey: ['org-companies'], queryFn: listCompanies });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['org-companies'] });

  const saveMutation = useMutation({
    mutationFn: async (values: { companyCode: string; name: string; taxCode?: string; parentCompanyId?: number }) => {
      if (editing) {
        return updateCompany(editing.id, { ...values, targetVersion: editing.rowVersion });
      }
      return createCompany(values);
    },
    onSuccess: () => {
      message.success(editing ? 'Đã cập nhật công ty' : 'Đã tạo công ty');
      setModalOpen(false);
      setEditing(null);
      form.resetFields();
      invalidate();
    },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e?.response?.data?.detail || 'Lưu công ty thất bại');
    },
  });

  const statusMutation = useMutation({
    mutationFn: (c: CompanyDto) => setCompanyStatus(c.id, !c.isActive, c.rowVersion),
    onSuccess: () => { message.success('Đã đổi trạng thái'); invalidate(); },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e?.response?.data?.detail || 'Đổi trạng thái thất bại');
    },
  });

  const openCreate = () => { setEditing(null); form.resetFields(); setModalOpen(true); };
  const openEdit = (c: CompanyDto) => {
    setEditing(c);
    form.setFieldsValue({ companyCode: c.companyCode, name: c.name, taxCode: c.taxCode, parentCompanyId: c.parentCompanyId });
    setModalOpen(true);
  };

  const columns = [
    { title: 'Mã', dataIndex: 'companyCode', key: 'companyCode' },
    { title: 'Tên công ty', dataIndex: 'name', key: 'name' },
    { title: 'Mã số thuế', dataIndex: 'taxCode', key: 'taxCode', render: (v: string | null) => v || '—' },
    {
      title: 'Công ty mẹ', dataIndex: 'parentCompanyId', key: 'parentCompanyId',
      render: (pid: number | null) => pid ? (companies?.find((x) => x.id === pid)?.name ?? pid) : '—',
    },
    {
      title: 'Trạng thái', dataIndex: 'isActive', key: 'isActive',
      render: (active: boolean) => <Tag color={active ? 'green' : 'red'}>{active ? 'Hoạt động' : 'Ngừng'}</Tag>,
    },
    {
      title: 'Thao tác', key: 'actions',
      render: (_: unknown, c: CompanyDto) => (
        <Space>
          <Button size="small" onClick={() => openEdit(c)}>Sửa</Button>
          <Button size="small" danger={c.isActive} onClick={() => statusMutation.mutate(c)} loading={statusMutation.isPending}>
            {c.isActive ? 'Ngừng' : 'Kích hoạt'}
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={3} style={{ margin: 0 }}>Công ty</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} data-testid="create-company-btn">Thêm công ty</Button>
      </Space>

      <Table rowKey="id" loading={isLoading} dataSource={companies} columns={columns} pagination={false} />

      <Modal
        title={editing ? 'Sửa công ty' : 'Thêm công ty'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditing(null); form.resetFields(); }}
        onOk={() => form.submit()}
        confirmLoading={saveMutation.isPending}
        okText="Lưu"
        cancelText="Hủy"
      >
        <Form form={form} layout="vertical" onFinish={(v) => saveMutation.mutate(v)}>
          <Form.Item name="companyCode" label="Mã công ty" rules={[{ required: true, message: 'Nhập mã công ty' }]}>
            <Input placeholder="VD: PTKD-HN" />
          </Form.Item>
          <Form.Item name="name" label="Tên công ty" rules={[{ required: true, message: 'Nhập tên công ty' }]}>
            <Input placeholder="VD: PTKD Hà Nội" />
          </Form.Item>
          <Form.Item name="taxCode" label="Mã số thuế">
            <Input />
          </Form.Item>
          <Form.Item name="parentCompanyId" label="Công ty mẹ (để trống nếu là tập đoàn/gốc)">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="— Không —"
              options={companies?.filter((c) => !editing || c.id !== editing.id).map((c) => ({ value: c.id, label: c.name }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default CompanyManagementPage;
