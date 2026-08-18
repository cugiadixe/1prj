import React, { useRef, useState } from 'react';
import { Alert, Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Spin, Table, Tag, message } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  createCustomerRelationship,
  deleteCustomerRelationship,
  getCustomerRelationships,
  getRelationshipKinds,
} from './relationshipsApi';
import { searchCustomers } from './customersApi';
import { getErrorMessage } from './errorMessages';
import type { CustomerRelationship } from './types';

interface Props {
  customerId: number;
  canManage: boolean;
}

interface CustOption {
  label: string;
  value: number;
}

const CustomerRelationshipsSection: React.FC<Props> = ({ customerId, canManage }) => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [modalOpen, setModalOpen] = useState(false);
  const [custOptions, setCustOptions] = useState<CustOption[]>([]);
  const [searching, setSearching] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const { data: relationships, isLoading } = useQuery({
    queryKey: ['customer-relationships', customerId],
    queryFn: () => getCustomerRelationships(customerId),
    enabled: !!customerId,
  });

  const { data: kinds } = useQuery({
    queryKey: ['relationship-kinds'],
    queryFn: getRelationshipKinds,
    staleTime: 30 * 60 * 1000,
  });

  const createMut = useMutation({
    mutationFn: (v: { otherCustomerId: number; relationKind: string; note?: string }) =>
      createCustomerRelationship(customerId, v),
    onSuccess: () => {
      message.success('Đã khai quan hệ.');
      setModalOpen(false);
      form.resetFields();
      setCustOptions([]);
      queryClient.invalidateQueries({ queryKey: ['customer-relationships', customerId] });
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const deleteMut = useMutation({
    mutationFn: (relId: number) => deleteCustomerRelationship(customerId, relId),
    onSuccess: () => {
      message.success('Đã xoá quan hệ.');
      queryClient.invalidateQueries({ queryKey: ['customer-relationships', customerId] });
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  // Tìm khách hàng người thân (debounce 300ms); loại chính khách đang xem khỏi kết quả.
  const handleSearch = (term: string) => {
    if (timer.current) clearTimeout(timer.current);
    if (!term || term.trim().length < 1) {
      setCustOptions([]);
      return;
    }
    timer.current = setTimeout(async () => {
      setSearching(true);
      try {
        const res = await searchCustomers({ search: term, pageSize: 10 });
        setCustOptions(
          res.items
            .filter((c) => c.id !== customerId)
            .map((c) => ({ label: `${c.fullName} (${c.customerCode})`, value: c.id })),
        );
      } catch {
        setCustOptions([]);
      } finally {
        setSearching(false);
      }
    }, 300);
  };

  const columns = [
    {
      title: 'Người thân',
      key: 'other',
      render: (_: unknown, r: CustomerRelationship) => (
        <Link to={`/customers/${r.otherCustomerId}`}>
          {r.otherCustomerName} ({r.otherCustomerCode})
        </Link>
      ),
    },
    {
      title: 'Là ... của khách này',
      key: 'rel',
      render: (_: unknown, r: CustomerRelationship) => (
        <Space size={4}>
          <span>{r.relationLabel}</span>
          {r.needsConfirmation && <Tag color="orange">cần xác nhận</Tag>}
          {r.isDerived && <Tag>suy diễn</Tag>}
        </Space>
      ),
    },
    { title: 'Ghi chú', dataIndex: 'note', key: 'note', render: (v: string | null) => v ?? '—' },
    ...(canManage
      ? [
          {
            title: 'Thao tác',
            key: 'action',
            render: (_: unknown, r: CustomerRelationship) => (
              <Popconfirm
                title="Xoá quan hệ này (cả chiều ngược lại)?"
                onConfirm={() => deleteMut.mutate(r.id)}
                okText="Xoá"
                cancelText="Huỷ"
              >
                <a data-testid={`delete-relationship-${r.id}`}>Xoá</a>
              </Popconfirm>
            ),
          },
        ]
      : []),
  ];

  return (
    <Card title="Quan hệ gia đình" style={{ marginBottom: 16 }} data-testid="customer-relationships-card">
      {canManage && (
        <Space style={{ marginBottom: 8 }}>
          <Button type="primary" size="small" onClick={() => setModalOpen(true)} data-testid="add-relationship-btn">
            Thêm quan hệ
          </Button>
        </Space>
      )}
      {isLoading && <Spin />}
      {relationships && relationships.length === 0 && (
        <Alert type="info" message="Chưa khai quan hệ gia đình cho khách hàng này." data-testid="no-relationships" />
      )}
      {relationships && relationships.length > 0 && (
        <Table
          dataSource={relationships}
          columns={columns}
          rowKey="id"
          pagination={false}
          size="small"
          data-testid="relationships-table"
        />
      )}

      <Modal
        title="Thêm quan hệ gia đình"
        open={modalOpen}
        onCancel={() => {
          setModalOpen(false);
          form.resetFields();
          setCustOptions([]);
        }}
        onOk={() => form.submit()}
        confirmLoading={createMut.isPending}
        okText="Lưu"
        cancelText="Huỷ"
        destroyOnHidden
      >
        <Form form={form} layout="vertical" onFinish={(v) => createMut.mutate(v)}>
          <Form.Item
            name="otherCustomerId"
            label="Người thân (khách hàng)"
            rules={[{ required: true, message: 'Chọn khách hàng người thân' }]}
          >
            <Select
              showSearch
              placeholder="Tìm theo tên/mã/CCCD..."
              filterOption={false}
              onSearch={handleSearch}
              notFoundContent={searching ? <Spin size="small" /> : null}
              options={custOptions}
              data-testid="relationship-customer-select"
            />
          </Form.Item>
          <Form.Item
            name="relationKind"
            label="Là ... của khách hàng này"
            rules={[{ required: true, message: 'Chọn loại quan hệ' }]}
          >
            <Select
              placeholder="VD: Mẹ, Con, Vợ/Chồng, Anh/Chị/Em..."
              options={(kinds ?? []).map((k) => ({ label: k.label, value: k.kindCode }))}
              data-testid="relationship-kind-select"
            />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default CustomerRelationshipsSection;
