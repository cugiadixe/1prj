import React, { useMemo, useState } from 'react';
import { Button, Input, Select, Space, Table, Tag, Typography, Alert } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getMyCustomerMasterChangeRequests } from './customerMasterChangeApi';
import type { CustomerMasterChangeDto } from './customerMasterChangeTypes';
import { getErrorMessage } from './errorMessages';

const { Title } = Typography;

// Nhãn + màu tiếng Việt cho trạng thái yêu cầu (khớp các giá trị backend).
const STATUS_META: Record<string, { label: string; color: string }> = {
  DRAFT: { label: 'Nháp', color: 'default' },
  SUBMITTED: { label: 'Chờ duyệt', color: 'processing' },
  APPROVED: { label: 'Đã duyệt', color: 'blue' },
  EXECUTED: { label: 'Đã áp dụng', color: 'success' },
  FAILED: { label: 'Thất bại', color: 'error' },
  WITHDRAWN: { label: 'Đã thu hồi', color: 'warning' },
};

function renderStatus(status: string) {
  const meta = STATUS_META[status] ?? { label: status, color: 'default' };
  return <Tag color={meta.color}>{meta.label}</Tag>;
}

function formatDate(text: string | null | undefined) {
  if (!text) return '—';
  return new Date(text).toLocaleString('vi-VN');
}

const CustomerMasterChangeRequestsPage: React.FC = () => {
  const { data: requests, isLoading, error } = useQuery({
    queryKey: ['my-change-requests'],
    queryFn: getMyCustomerMasterChangeRequests,
  });

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);

  // Các trạng thái thực có trong dữ liệu, để đổ vào bộ lọc.
  const statusOptions = useMemo(() => {
    const set = new Set((requests ?? []).map((r) => r.requestStatus));
    return Array.from(set).map((s) => ({
      value: s,
      label: STATUS_META[s]?.label ?? s,
    }));
  }, [requests]);

  const filtered = useMemo(() => {
    const kw = search.trim().toLowerCase();
    return (requests ?? []).filter((r) => {
      if (statusFilter && r.requestStatus !== statusFilter) return false;
      if (!kw) return true;
      const haystack = [
        r.processCode,
        r.targetCustomerCode,
        r.targetCustomerName,
        r.targetCustomerId?.toString(),
        r.payload?.reason,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();
      return haystack.includes(kw);
    });
  }, [requests, search, statusFilter]);

  const columns = [
    {
      title: 'Mã hồ sơ',
      dataIndex: 'processCode',
      key: 'processCode',
      width: 140,
    },
    {
      title: 'Khách hàng',
      key: 'customer',
      render: (_: unknown, r: CustomerMasterChangeDto) => (
        <Space direction="vertical" size={0}>
          <span style={{ fontWeight: 500 }}>{r.targetCustomerName ?? '—'}</span>
          <span style={{ color: '#888', fontSize: 12 }}>
            {r.targetCustomerCode ?? (r.targetCustomerId ? `#${r.targetCustomerId}` : '—')}
          </span>
        </Space>
      ),
    },
    {
      title: 'Lý do',
      key: 'reason',
      render: (_: unknown, r: CustomerMasterChangeDto) => r.payload?.reason ?? '—',
      ellipsis: true,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'requestStatus',
      key: 'requestStatus',
      width: 130,
      render: (status: string) => renderStatus(status),
    },
    {
      title: 'Ngày gửi',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (text: string) => formatDate(text),
    },
    {
      title: 'Cập nhật',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      width: 160,
      render: (text: string | null) => formatDate(text),
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 200,
      render: (_: unknown, record: CustomerMasterChangeDto) => (
        <Space size="middle">
          <Link to={`/customers/change-requests/${record.id}`}>Xem trạng thái</Link>
          {record.workflowInstanceId && (
            <Link to={`/workflow/instances/${record.workflowInstanceId}`}>Xem quy trình</Link>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="customer-master-change-requests-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Danh sách khách hàng yêu cầu thay đổi</Title>
        <Space>
          <Button>
            <Link to="/customers">Quay lại khách hàng</Link>
          </Button>
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }} wrap>
        <Input.Search
          data-testid="change-request-search"
          placeholder="Tìm theo mã hồ sơ, mã/tên KH, lý do..."
          allowClear
          style={{ width: 320 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          data-testid="change-request-status-filter"
          placeholder="Trạng thái"
          allowClear
          style={{ width: 200 }}
          value={statusFilter}
          onChange={(v) => setStatusFilter(v)}
          options={statusOptions}
        />
      </Space>

      {error && (
        <Alert
          type="error"
          message={getErrorMessage(error) || 'Không thể tải danh sách yêu cầu thay đổi'}
          style={{ marginBottom: 16 }}
        />
      )}

      <Table
        columns={columns}
        dataSource={filtered}
        rowKey="id"
        loading={isLoading}
        pagination={{ pageSize: 20 }}
      />
    </div>
  );
};

export default CustomerMasterChangeRequestsPage;
