import React, { useRef, useState } from 'react';
import { Alert, Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Spin, Table, Tag, Typography, message } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { searchCustomers } from './customersApi';
import {
  createCustomerRelationship,
  deleteCustomerRelationship,
  getRelationshipKinds,
  searchRelationships,
} from './relationshipsApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { RelationshipListItem } from './types';

const { Title } = Typography;

interface CustOption {
  label: string;
  value: number;
}

/** Ô chọn khách hàng có tìm kiếm (debounce). excludeId để loại một người khỏi kết quả. */
const CustomerSearchSelect: React.FC<{
  value?: number;
  onChange?: (v: number | undefined) => void;
  excludeId?: number;
  placeholder?: string;
}> = ({ value, onChange, excludeId, placeholder }) => {
  const [options, setOptions] = useState<CustOption[]>([]);
  const [loading, setLoading] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const handleSearch = (term: string) => {
    if (timer.current) clearTimeout(timer.current);
    if (!term || term.trim().length < 1) {
      setOptions([]);
      return;
    }
    timer.current = setTimeout(async () => {
      setLoading(true);
      try {
        const res = await searchCustomers({ search: term, pageSize: 10 });
        setOptions(
          res.items
            .filter((c) => c.id !== excludeId)
            .map((c) => ({ label: `${c.fullName} (${c.customerCode})`, value: c.id })),
        );
      } catch {
        setOptions([]);
      } finally {
        setLoading(false);
      }
    }, 300);
  };

  return (
    <Select
      showSearch
      allowClear
      value={value}
      onChange={onChange}
      placeholder={placeholder ?? 'Tìm theo tên/mã/CCCD...'}
      filterOption={false}
      onSearch={handleSearch}
      notFoundContent={loading ? <Spin size="small" /> : null}
      options={options}
    />
  );
};

const RelationshipsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const canManage = hasPermission('CUSTOMER_RELATIONSHIP_MANAGE');

  const [search, setSearch] = useState('');
  const [kindFilter, setKindFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [addOpen, setAddOpen] = useState(false);
  const [editing, setEditing] = useState<RelationshipListItem | null>(null);
  const [addForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const addFromId = Form.useWatch('fromCustomerId', addForm) as number | undefined;

  const { data, isLoading, error } = useQuery({
    queryKey: ['relationships', search, kindFilter, page, pageSize],
    queryFn: () => searchRelationships({ search, kind: kindFilter, page, pageSize }),
  });

  const { data: kinds } = useQuery({
    queryKey: ['relationship-kinds'],
    queryFn: getRelationshipKinds,
    staleTime: 30 * 60 * 1000,
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['relationships'] });

  const addMut = useMutation({
    mutationFn: (v: { fromCustomerId: number; otherCustomerId: number; relationKind: string; note?: string }) =>
      createCustomerRelationship(v.fromCustomerId, {
        otherCustomerId: v.otherCustomerId,
        relationKind: v.relationKind,
        note: v.note,
      }),
    onSuccess: () => {
      message.success('Đã thêm quan hệ.');
      setAddOpen(false);
      addForm.resetFields();
      invalidate();
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const editMut = useMutation({
    mutationFn: (v: { fromCustomerId: number; otherCustomerId: number; relationKind: string; note?: string }) =>
      createCustomerRelationship(v.fromCustomerId, {
        otherCustomerId: v.otherCustomerId,
        relationKind: v.relationKind,
        note: v.note,
      }),
    onSuccess: () => {
      message.success('Đã cập nhật quan hệ.');
      setEditing(null);
      editForm.resetFields();
      invalidate();
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const deleteMut = useMutation({
    mutationFn: (r: RelationshipListItem) => deleteCustomerRelationship(r.fromCustomerId, r.id),
    onSuccess: () => {
      message.success('Đã xoá quan hệ.');
      invalidate();
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const openEdit = (r: RelationshipListItem) => {
    setEditing(r);
    editForm.setFieldsValue({ relationKind: r.relationKind, note: r.note ?? undefined });
  };

  const kindOptions = (kinds ?? []).map((k) => ({ label: k.label, value: k.kindCode }));

  if (isPermissionDenied(error)) {
    return <Alert type="error" message="Bạn không có quyền xem quan hệ gia đình." data-testid="permission-denied" />;
  }

  const columns = [
    {
      title: 'Người A',
      key: 'from',
      render: (_: unknown, r: RelationshipListItem) => (
        <Link to={`/customers/${r.fromCustomerId}`}>{r.fromCustomerName} ({r.fromCustomerCode})</Link>
      ),
    },
    {
      title: 'Người B',
      key: 'to',
      render: (_: unknown, r: RelationshipListItem) => (
        <Link to={`/customers/${r.toCustomerId}`}>{r.toCustomerName} ({r.toCustomerCode})</Link>
      ),
    },
    {
      title: 'Quan hệ',
      key: 'rel',
      render: (_: unknown, r: RelationshipListItem) => (
        <Space size={4}>
          <span>B là <b>{r.relationLabel}</b> của A</span>
          {r.needsConfirmation && <Tag color="orange">cần xác nhận</Tag>}
        </Space>
      ),
    },
    { title: 'Ghi chú', dataIndex: 'note', key: 'note', render: (v: string | null) => v ?? '—' },
    ...(canManage
      ? [
          {
            title: 'Thao tác',
            key: 'action',
            render: (_: unknown, r: RelationshipListItem) => (
              <Space>
                <a onClick={() => openEdit(r)} data-testid={`edit-rel-${r.id}`}>Sửa</a>
                <Popconfirm
                  title="Xoá quan hệ này (cả chiều ngược lại)?"
                  onConfirm={() => deleteMut.mutate(r)}
                  okText="Xoá"
                  cancelText="Huỷ"
                >
                  <a data-testid={`delete-rel-${r.id}`}>Xoá</a>
                </Popconfirm>
              </Space>
            ),
          },
        ]
      : []),
  ];

  return (
    <div data-testid="relationships-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Quan hệ gia đình</Title>
        {canManage && (
          <Button type="primary" onClick={() => setAddOpen(true)} data-testid="add-relationship-btn">
            Thêm quan hệ
          </Button>
        )}
      </Space>

      <Space style={{ marginBottom: 16 }} wrap>
        <Input.Search
          placeholder="Tìm theo tên/mã khách..."
          allowClear
          onSearch={(v) => { setSearch(v); setPage(1); }}
          style={{ width: 280 }}
          data-testid="relationship-search"
        />
        <Select
          placeholder="Loại quan hệ"
          allowClear
          style={{ width: 200 }}
          value={kindFilter}
          onChange={(v) => { setKindFilter(v); setPage(1); }}
          options={kindOptions}
          data-testid="relationship-kind-filter"
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert type="error" message={getErrorMessage(error)} style={{ marginBottom: 16 }} />
      )}

      <Card>
        {isLoading && <Spin data-testid="relationships-loading" />}
        {data && data.items.length === 0 && !isLoading && (
          <Alert type="info" message="Chưa có quan hệ nào." data-testid="relationships-empty" />
        )}
        {data && data.items.length > 0 && (
          <Table
            dataSource={data.items}
            columns={columns}
            rowKey="id"
            size="small"
            data-testid="relationships-table"
            pagination={{
              current: data.page,
              pageSize: data.pageSize,
              total: data.totalCount,
              onChange: (p, ps) => { setPage(p); setPageSize(ps); },
            }}
          />
        )}
      </Card>

      {/* Thêm quan hệ */}
      <Modal
        title="Thêm quan hệ gia đình"
        open={addOpen}
        onCancel={() => { setAddOpen(false); addForm.resetFields(); }}
        onOk={() => addForm.submit()}
        confirmLoading={addMut.isPending}
        okText="Lưu"
        cancelText="Huỷ"
        destroyOnHidden
      >
        <Form form={addForm} layout="vertical" onFinish={(v) => addMut.mutate(v)}>
          <Form.Item name="fromCustomerId" label="Người A (khách hàng)" rules={[{ required: true, message: 'Chọn người A' }]}>
            <CustomerSearchSelect placeholder="Tìm người A..." />
          </Form.Item>
          <Form.Item
            name="otherCustomerId"
            label="Người B (người thân)"
            dependencies={['fromCustomerId']}
            rules={[
              { required: true, message: 'Chọn người B' },
              ({ getFieldValue }) => ({
                validator: (_, v) =>
                  !v || v !== getFieldValue('fromCustomerId')
                    ? Promise.resolve()
                    : Promise.reject(new Error('Người B phải khác người A')),
              }),
            ]}
          >
            <CustomerSearchSelect placeholder="Tìm người B..." excludeId={addFromId} />
          </Form.Item>
          <Form.Item name="relationKind" label="Người B là ... của người A" rules={[{ required: true, message: 'Chọn quan hệ' }]}>
            <Select placeholder="VD: Mẹ, Con, Vợ/Chồng..." options={kindOptions} />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>

      {/* Sửa quan hệ */}
      <Modal
        title="Sửa quan hệ gia đình"
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
            onFinish={(v) =>
              editMut.mutate({
                fromCustomerId: editing.fromCustomerId,
                otherCustomerId: editing.toCustomerId,
                relationKind: v.relationKind,
                note: v.note,
              })
            }
          >
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message={`${editing.toCustomerName} là ... của ${editing.fromCustomerName}`}
              description="Đổi người thì xoá rồi thêm lại; ở đây chỉ đổi loại quan hệ / ghi chú."
            />
            <Form.Item name="relationKind" label="Người B là ... của người A" rules={[{ required: true, message: 'Chọn quan hệ' }]}>
              <Select options={kindOptions} />
            </Form.Item>
            <Form.Item name="note" label="Ghi chú">
              <Input.TextArea rows={2} />
            </Form.Item>
          </Form>
        )}
      </Modal>
    </div>
  );
};

export default RelationshipsPage;
