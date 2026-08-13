import React, { useState } from 'react';
import {
  Alert, Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Table, Tabs, Tag as AntTag, Typography, message,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, TagsOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usePermissions } from '../auth/AuthProvider';
import { createTag, deactivateTag, listTags, updateTag } from './tagsApi';
import type { Tag, TagType } from './types';
import { DEFAULT_TAG_COLOR } from './types';

const { Title } = Typography;

// Bảng màu preset của Ant Design (khớp backend).
const PRESET_COLORS = [
  'magenta', 'red', 'volcano', 'orange', 'gold', 'lime', 'green', 'cyan', 'blue', 'geekblue', 'purple',
];

const colorOptions = PRESET_COLORS.map((c) => ({
  value: c,
  label: <AntTag color={c} style={{ marginInlineEnd: 0 }}>{c}</AntTag>,
}));

const TagCatalog: React.FC<{ tagType: TagType }> = ({ tagType }) => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Tag | null>(null);

  const { data: tags, isLoading } = useQuery({
    queryKey: ['tags', tagType, 'manage'],
    queryFn: () => listTags(tagType, true),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['tags'] });

  const saveMutation = useMutation({
    mutationFn: (values: { name: string; color: string; isActive?: boolean }) =>
      editing
        ? updateTag(editing.id, {
          name: values.name, color: values.color,
          isActive: values.isActive ?? true, targetVersion: editing.rowVersion ?? '',
        })
        : createTag(tagType, values.name, values.color),
    onSuccess: () => {
      message.success(editing ? 'Đã cập nhật thẻ' : 'Đã tạo thẻ');
      setModalOpen(false); setEditing(null); form.resetFields();
      invalidate();
    },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e.response?.data?.detail ?? 'Lưu thẻ thất bại');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deactivateTag(id),
    onSuccess: () => { message.success('Đã xóa thẻ'); invalidate(); },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string } } };
      message.error(e.response?.data?.detail ?? 'Xóa thẻ thất bại');
    },
  });

  const openCreate = () => { setEditing(null); form.resetFields(); form.setFieldsValue({ color: DEFAULT_TAG_COLOR }); setModalOpen(true); };
  const openEdit = (t: Tag) => {
    setEditing(t);
    form.setFieldsValue({ name: t.name, color: t.color ?? DEFAULT_TAG_COLOR, isActive: t.isActive });
    setModalOpen(true);
  };

  return (
    <>
      <Space style={{ marginBottom: 12 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate} data-testid={`create-tag-${tagType}`}>
          Tạo thẻ
        </Button>
      </Space>
      <Table
        dataSource={tags}
        loading={isLoading}
        rowKey="id"
        pagination={false}
        size="small"
        columns={[
          {
            title: 'Thẻ', key: 'name',
            render: (_: unknown, t: Tag) => (
              <AntTag color={t.color ?? DEFAULT_TAG_COLOR}>#{t.name}</AntTag>
            ),
          },
          { title: 'Màu', dataIndex: 'color', key: 'color', render: (c: string | null) => c ?? '—' },
          {
            title: 'Số lượng gắn', dataIndex: 'usageCount', key: 'usageCount',
            render: (n: number | undefined) => n ?? 0,
          },
          {
            title: 'Trạng thái', dataIndex: 'isActive', key: 'isActive',
            render: (a: boolean) => (a ? <AntTag color="green">Đang dùng</AntTag> : <AntTag>Đã ẩn</AntTag>),
          },
          {
            title: '', key: 'actions', width: 90,
            render: (_: unknown, t: Tag) => (
              <Space size={0}>
                <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(t)} data-testid={`edit-tag-${t.id}`} />
                <Popconfirm
                  title="Xóa thẻ này?"
                  description={t.usageCount ? `Thẻ đang gắn ${t.usageCount} nơi — sẽ gỡ khỏi tất cả.` : undefined}
                  okText="Xóa" cancelText="Hủy"
                  onConfirm={() => deleteMutation.mutate(t.id)}
                >
                  <Button type="text" danger icon={<DeleteOutlined />} data-testid={`delete-tag-${t.id}`} />
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Sửa thẻ' : 'Tạo thẻ mới'}
        open={modalOpen}
        onCancel={() => { setModalOpen(false); setEditing(null); }}
        onOk={() => form.submit()}
        confirmLoading={saveMutation.isPending}
        okText="Lưu" cancelText="Hủy"
        destroyOnHidden
      >
        <Form form={form} layout="vertical" onFinish={(v) => saveMutation.mutate(v)}>
          <Form.Item name="name" label="Tên thẻ" rules={[{ required: true, message: 'Nhập tên thẻ' }, { max: 50 }]}>
            <Input prefix="#" placeholder="VD: VIP" />
          </Form.Item>
          <Form.Item name="color" label="Màu" rules={[{ required: true }]}>
            <Select options={colorOptions} />
          </Form.Item>
          {editing && (
            <Form.Item name="isActive" label="Trạng thái">
              <Select options={[{ value: true, label: 'Đang dùng' }, { value: false, label: 'Đã ẩn' }]} />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </>
  );
};

const TagManagementPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  if (!hasPermission('TAG_MANAGE', 'GLOBAL')) {
    return <Alert type="error" message="Bạn không có quyền quản lý thẻ." data-testid="permission-denied" />;
  }

  return (
    <div data-testid="tag-management-page">
      <Title level={4} style={{ marginTop: 0 }}><Space><TagsOutlined />Quản lý thẻ</Space></Title>
      <Card>
        <Tabs
          items={[
            { key: 'CUSTOMER', label: 'Thẻ khách hàng', children: <TagCatalog tagType="CUSTOMER" /> },
            { key: 'GRAVE', label: 'Thẻ phần mộ', children: <TagCatalog tagType="GRAVE" /> },
          ]}
        />
      </Card>
    </div>
  );
};

export default TagManagementPage;
