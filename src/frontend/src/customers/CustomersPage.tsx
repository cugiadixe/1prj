import React, { useState } from 'react';
import { Alert, Button, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getCompanyLookups, getStaffLookups, searchCustomers } from './customersApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import { CUSTOMER_STATUS_COLORS, CUSTOMER_STATUS_LABELS, type CustomerListItem } from './types';
import { listTags } from '../tags/tagsApi';
import TagChips from '../tags/TagChips';

const { Title } = Typography;

const UNASSIGNED_STAFF = 'UNASSIGNED';

const CustomersPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [companyFilter, setCompanyFilter] = useState<number | undefined>(undefined);
  const [staffFilter, setStaffFilter] = useState<number | typeof UNASSIGNED_STAFF | undefined>(undefined);
  const [tagFilter, setTagFilter] = useState<number[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const assignedStaffId = typeof staffFilter === 'number' ? staffFilter : undefined;
  const unassignedStaff = staffFilter === UNASSIGNED_STAFF;

  const { data, isLoading, error } = useQuery({
    queryKey: ['customers', search, statusFilter, companyFilter, staffFilter, tagFilter, page, pageSize],
    queryFn: () =>
      searchCustomers({
        search,
        customerStatus: statusFilter,
        companyId: companyFilter,
        assignedStaffId,
        unassignedStaff,
        tagIds: tagFilter,
        page,
        pageSize,
      }),
  });

  const { data: tagOptions } = useQuery({
    queryKey: ['tags', 'CUSTOMER'],
    queryFn: () => listTags('CUSTOMER'),
    staleTime: 5 * 60 * 1000,
  });

  const { data: companyOptions } = useQuery({
    queryKey: ['customer-company-lookups'],
    queryFn: getCompanyLookups,
    staleTime: 5 * 60 * 1000,
  });

  const { data: staffOptions } = useQuery({
    queryKey: ['customer-staff-lookups'],
    queryFn: getStaffLookups,
    staleTime: 5 * 60 * 1000,
  });

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem khách hàng."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Mã',
      dataIndex: 'customerCode',
      key: 'customerCode',
    },
    {
      title: 'Họ và tên',
      dataIndex: 'fullName',
      key: 'fullName',
    },
    {
      title: 'CCCD',
      dataIndex: 'cccd',
      key: 'cccd',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Điện thoại',
      dataIndex: 'phone',
      key: 'phone',
      render: (val: string | null) => val ?? '—',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'customerStatus',
      key: 'customerStatus',
      render: (status: string) => (
        <Tag color={CUSTOMER_STATUS_COLORS[status] ?? 'default'}>
          {CUSTOMER_STATUS_LABELS[status] ?? status}
        </Tag>
      ),
    },
    {
      title: 'Thẻ',
      key: 'tags',
      render: (_: unknown, r: CustomerListItem) => <TagChips tags={r.tags} size="small" />,
    },
  ];

  return (
    <div data-testid="customers-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Khách hàng</Title>
        <Space>
          {hasPermission('CUSTOMER_CREATE_FINAL', 'GLOBAL') && (
            <Button type="primary" data-testid="create-customer-btn">
              <Link to="/customers/new">Tạo khách hàng</Link>
            </Button>
          )}
          {hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL') && (
            <Button type="default" data-testid="submit-customer-proposal-btn">
              <Link to="/customers/proposals/new">Gửi đề xuất</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Tìm theo tên, mã, CCCD..."
          allowClear
          onSearch={(val) => { setSearch(val); setPage(1); }}
          style={{ width: 300 }}
          data-testid="customer-search"
        />
        <Select
          placeholder="Trạng thái"
          allowClear
          style={{ width: 150 }}
          onChange={(val) => { setStatusFilter(val); setPage(1); }}
          value={statusFilter}
          data-testid="customer-status-filter"
          options={[
            { label: 'Hoạt động', value: 'ACTIVE' },
            { label: 'Ngừng HĐ', value: 'INACTIVE' },
            { label: 'Đã gộp', value: 'MERGED' },
          ]}
        />
        <Select
          placeholder="Công ty phụ trách"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ width: 200 }}
          onChange={(val) => { setCompanyFilter(val); setPage(1); }}
          value={companyFilter}
          data-testid="customer-company-filter"
          options={(companyOptions ?? []).map((c) => ({ label: c.name, value: c.id }))}
        />
        <Select
          placeholder="Nhân viên phụ trách"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ width: 200 }}
          onChange={(val) => { setStaffFilter(val); setPage(1); }}
          value={staffFilter}
          data-testid="customer-staff-filter"
          options={[
            { label: '— (chưa phân)', value: UNASSIGNED_STAFF },
            ...(staffOptions ?? []).map((s) => ({ label: s.fullName, value: s.id })),
          ]}
        />
        <Select
          mode="multiple"
          placeholder="Lọc theo thẻ"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ minWidth: 200 }}
          onChange={(val: number[]) => { setTagFilter(val); setPage(1); }}
          value={tagFilter}
          data-testid="customer-tag-filter"
          options={(tagOptions ?? []).map((t) => ({ label: `#${t.name}`, value: t.id }))}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="customer-list-error"
        />
      )}

      {isLoading && <Spin data-testid="customer-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="Không tìm thấy khách hàng."
          data-testid="customer-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="customer-list-table"
          onRow={(record: CustomerListItem) => ({
            onClick: () => navigate(`/customers/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          pagination={{
            current: data.page,
            pageSize: data.pageSize,
            total: data.totalCount,
            onChange: (p, ps) => { setPage(p); setPageSize(ps); },
          }}
        />
      )}
    </div>
  );
};

export default CustomersPage;
