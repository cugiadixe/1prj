import React, { useState } from 'react';
import {
  Alert, Button, Card, Divider, Form, Input, InputNumber, Modal, Popconfirm, Space, Spin, Switch, Table, Tag, Tooltip, Typography, message,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usePermissions } from '../auth/AuthProvider';
import {
  createRelationshipKind,
  deleteRelationshipKind,
  getAllRelationshipKinds,
  updateRelationshipKind,
} from './relationshipKindsApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { RelationshipKindDetail } from './types';

const { Title } = Typography;

const RelationshipKindsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const canManage = hasPermission('RELATIONSHIP_KIND_MANAGE');

  const [addOpen, setAddOpen] = useState(false);
  const [editing, setEditing] = useState<RelationshipKindDetail | null>(null);
  const [addForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const isSymmetric = Form.useWatch('isSymmetric', addForm) as boolean | undefined;

  const { data: kinds, isLoading, error } = useQuery({
    queryKey: ['relationship-kinds-manage'],
    queryFn: getAllRelationshipKinds,
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['relationship-kinds-manage'] });
    queryClient.invalidateQueries({ queryKey: ['relationship-kinds'] }); // dropdown khai quan hệ
  };

  const addMut = useMutation({
    mutationFn: createRelationshipKind,
    onSuccess: () => {
      message.success('Đã thêm loại quan hệ.');
      setAddOpen(false);
      addForm.resetFields();
      invalidate();
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const editMut = useMutation({
    mutationFn: (v: { kindCode: string; labelMale: string; labelFemale: string; labelNeutral: string; sortOrder: number }) =>
      updateRelationshipKind(v.kindCode, {
        labelMale: v.labelMale, labelFemale: v.labelFemale, labelNeutral: v.labelNeutral, sortOrder: v.sortOrder,
      }),
    onSuccess: () => {
      message.success('Đã cập nhật loại quan hệ.');
      setEditing(null);
      editForm.resetFields();
      invalidate();
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const deleteMut = useMutation({
    mutationFn: (code: string) => deleteRelationshipKind(code),
    onSuccess: () => {
      message.success('Đã xoá loại quan hệ.');
      invalidate();
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const openEdit = (k: RelationshipKindDetail) => {
    setEditing(k);
    editForm.setFieldsValue({
      labelMale: k.labelMale, labelFemale: k.labelFemale, labelNeutral: k.labelNeutral, sortOrder: k.sortOrder,
    });
  };

  if (isPermissionDenied(error)) {
    return <Alert type="error" message="Bạn không có quyền quản lý loại quan hệ." data-testid="permission-denied" />;
  }

  const columns = [
    { title: 'Nhãn chung', dataIndex: 'labelNeutral', key: 'labelNeutral', render: (v: string) => <b>{v}</b> },
    { title: 'Nam', dataIndex: 'labelMale', key: 'labelMale' },
    { title: 'Nữ', dataIndex: 'labelFemale', key: 'labelFemale' },
    {
      title: 'Nghịch đảo',
      key: 'inverse',
      render: (_: unknown, k: RelationshipKindDetail) =>
        k.isSymmetric ? <Tag>tự nghịch đảo</Tag> : (k.inverseLabelNeutral ?? '—'),
    },
    { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder', width: 80 },
    {
      title: 'Loại',
      key: 'core',
      render: (_: unknown, k: RelationshipKindDetail) =>
        k.isCore ? <Tag color="blue">hệ thống</Tag> : <Tag color="green">tuỳ chỉnh</Tag>,
    },
    ...(canManage
      ? [
          {
            title: 'Thao tác',
            key: 'action',
            render: (_: unknown, k: RelationshipKindDetail) => (
              <Space>
                <a onClick={() => openEdit(k)} data-testid={`edit-kind-${k.kindCode}`}>Sửa</a>
                {k.deletable ? (
                  <Popconfirm
                    title="Xoá loại quan hệ này (cả vế nghịch đảo)?"
                    onConfirm={() => deleteMut.mutate(k.kindCode)}
                    okText="Xoá"
                    cancelText="Huỷ"
                  >
                    <a data-testid={`delete-kind-${k.kindCode}`}>Xoá</a>
                  </Popconfirm>
                ) : (
                  <Tooltip title={k.isCore ? 'Loại hệ thống, không xoá được' : 'Đang được dùng, không xoá được'}>
                    <span style={{ color: '#bbb', cursor: 'not-allowed' }}>Xoá</span>
                  </Tooltip>
                )}
              </Space>
            ),
          },
        ]
      : []),
  ];

  return (
    <div data-testid="relationship-kinds-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Loại quan hệ</Title>
        {canManage && (
          <Button type="primary" onClick={() => setAddOpen(true)} data-testid="add-kind-btn">
            Thêm loại quan hệ
          </Button>
        )}
      </Space>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Danh mục này dùng chung toàn hệ — thêm loại mới thì mọi nơi khai quan hệ đều có thêm lựa chọn đó."
      />

      <Card>
        {isLoading && <Spin data-testid="kinds-loading" />}
        {kinds && kinds.length > 0 && (
          <Table dataSource={kinds} columns={columns} rowKey="kindCode" size="small" pagination={false} data-testid="kinds-table" />
        )}
      </Card>

      {/* Thêm loại quan hệ */}
      <Modal
        title="Thêm loại quan hệ"
        open={addOpen}
        onCancel={() => { setAddOpen(false); addForm.resetFields(); }}
        onOk={() => addForm.submit()}
        confirmLoading={addMut.isPending}
        okText="Lưu"
        cancelText="Huỷ"
        destroyOnHidden
        width={640}
      >
        <Form form={addForm} layout="vertical" initialValues={{ isSymmetric: false, sortOrder: 100 }} onFinish={(v) => addMut.mutate(v)}>
          <Form.Item
            name="isSymmetric"
            label="Đối xứng (hai vế cùng một loại, tự đảo — vd Vợ/Chồng, Anh-Chị-Em)"
            valuePropName="checked"
          >
            <Switch />
          </Form.Item>

          <Divider plain>{isSymmetric ? 'Nhãn (theo giới tính)' : 'Vế A'}</Divider>
          <Space size="large" wrap>
            <Form.Item name={['sideA', 'labelMale']} label="Nhãn Nam" rules={[{ required: true, message: 'Nhập nhãn Nam' }]}>
              <Input placeholder="VD: Chồng / Bố dượng" />
            </Form.Item>
            <Form.Item name={['sideA', 'labelFemale']} label="Nhãn Nữ" rules={[{ required: true, message: 'Nhập nhãn Nữ' }]}>
              <Input placeholder="VD: Vợ / Mẹ kế" />
            </Form.Item>
            <Form.Item name={['sideA', 'labelNeutral']} label="Nhãn chung" rules={[{ required: true, message: 'Nhập nhãn chung' }]}>
              <Input placeholder="VD: Vợ/Chồng / Cha-Mẹ kế" />
            </Form.Item>
          </Space>

          {!isSymmetric && (
            <>
              <Divider plain>Vế nghịch đảo (B)</Divider>
              <Space size="large" wrap>
                <Form.Item name={['sideB', 'labelMale']} label="Nhãn Nam" rules={[{ required: true, message: 'Nhập nhãn Nam' }]}>
                  <Input placeholder="VD: Con riêng (trai)" />
                </Form.Item>
                <Form.Item name={['sideB', 'labelFemale']} label="Nhãn Nữ" rules={[{ required: true, message: 'Nhập nhãn Nữ' }]}>
                  <Input placeholder="VD: Con riêng (gái)" />
                </Form.Item>
                <Form.Item name={['sideB', 'labelNeutral']} label="Nhãn chung" rules={[{ required: true, message: 'Nhập nhãn chung' }]}>
                  <Input placeholder="VD: Con riêng" />
                </Form.Item>
              </Space>
            </>
          )}

          <Form.Item name="sortOrder" label="Thứ tự hiển thị">
            <InputNumber min={0} />
          </Form.Item>
        </Form>
      </Modal>

      {/* Sửa loại quan hệ */}
      <Modal
        title="Sửa loại quan hệ"
        open={!!editing}
        onCancel={() => { setEditing(null); editForm.resetFields(); }}
        onOk={() => editForm.submit()}
        confirmLoading={editMut.isPending}
        okText="Lưu"
        cancelText="Huỷ"
        destroyOnHidden
      >
        {editing && (
          <Form
            form={editForm}
            layout="vertical"
            onFinish={(v) => editMut.mutate({ kindCode: editing.kindCode, ...v })}
          >
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message="Chỉ sửa nhãn + thứ tự của vế này. Sửa vế nghịch đảo thì sửa dòng tương ứng; đổi cấu trúc thì xoá và tạo lại."
            />
            <Form.Item name="labelMale" label="Nhãn Nam" rules={[{ required: true, message: 'Nhập nhãn Nam' }]}>
              <Input />
            </Form.Item>
            <Form.Item name="labelFemale" label="Nhãn Nữ" rules={[{ required: true, message: 'Nhập nhãn Nữ' }]}>
              <Input />
            </Form.Item>
            <Form.Item name="labelNeutral" label="Nhãn chung" rules={[{ required: true, message: 'Nhập nhãn chung' }]}>
              <Input />
            </Form.Item>
            <Form.Item name="sortOrder" label="Thứ tự hiển thị">
              <InputNumber min={0} />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </div>
  );
};

export default RelationshipKindsPage;
