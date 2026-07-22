import React, { useState } from 'react';
import {
  Alert,
  Button,
  Input,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Typography,
} from 'antd';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { searchAccounts } from '../accountManagement/accountManagementApi';
import {
  PERMISSION_DENIED,
  GENERIC_ERROR,
  isPermissionDenied,
} from '../accountManagement/errorMessages';
import type { AccountSummaryDto, AccountStatus } from '../accountManagement/types';

const { Title } = Typography;
const { Search } = Input;
const { Option } = Select;

const STATUS_COLORS: Record<string, string> = {
  ACTIVE: 'green',
  LOCKED: 'orange',
  DISABLED: 'red',
};

const PAGE_SIZE = 20;

const AccountManagementPage: React.FC = () => {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [providerTypeFilter, setProviderTypeFilter] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['accounts', search, statusFilter, providerTypeFilter, page],
    queryFn: () =>
      searchAccounts({
        search: search || undefined,
        status: statusFilter,
        providerType: providerTypeFilter,
        page,
        pageSize: PAGE_SIZE,
      }),
    retry: false,
  });

  const handleSearch = (value: string) => {
    setSearch(value);
    setPage(1);
  };

  const handleStatusChange = (value: string | undefined) => {
    setStatusFilter(value);
    setPage(1);
  };

  const handleProviderTypeChange = (value: string | undefined) => {
    setProviderTypeFilter(value);
    setPage(1);
  };

  const columns = [
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: AccountStatus) => (
        <Tag color={STATUS_COLORS[status] ?? 'default'} data-testid={`status-badge-${status}`}>
          {status}
        </Tag>
      ),
    },
    {
      title: 'Username',
      dataIndex: 'username',
      key: 'username',
    },
    {
      title: 'Full Name',
      dataIndex: 'fullName',
      key: 'fullName',
    },
    {
      title: 'Employee Code',
      dataIndex: 'employeeCode',
      key: 'employeeCode',
    },
    {
      title: 'Provider Type',
      dataIndex: 'providerType',
      key: 'providerType',
    },
    {
      title: 'Employment Status',
      dataIndex: 'employmentStatus',
      key: 'employmentStatus',
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: AccountSummaryDto) => (
        <Button
          type="link"
          size="small"
          data-testid={`manage-account-${record.accountId}`}
          onClick={() => navigate(`/security/accounts/${record.accountId}`)}
        >
          Manage
        </Button>
      ),
    },
  ];

  // Error states
  if (isError) {
    const message = isPermissionDenied(error)
      ? PERMISSION_DENIED
      : GENERIC_ERROR;
    return (
      <div data-testid="account-list-error">
        <Alert
          type={isPermissionDenied(error) ? 'warning' : 'error'}
          message={message}
          data-testid="account-list-error-message"
        />
      </div>
    );
  }

  return (
    <div data-testid="account-management-page">
      <Title level={3}>Account Management</Title>

      <Space style={{ marginBottom: 16 }} wrap>
        <Search
          placeholder="Search by username, employee code, or name"
          allowClear
          onSearch={handleSearch}
          style={{ width: 320 }}
          data-testid="account-search-input"
          aria-label="Search accounts"
        />

        <Select
          allowClear
          placeholder="Filter by status"
          style={{ width: 160 }}
          value={statusFilter}
          onChange={handleStatusChange}
          data-testid="status-filter"
          aria-label="Filter by status"
        >
          <Option value="ACTIVE">ACTIVE</Option>
          <Option value="LOCKED">LOCKED</Option>
          <Option value="DISABLED">DISABLED</Option>
        </Select>

        <Select
          allowClear
          placeholder="Filter by provider"
          style={{ width: 160 }}
          value={providerTypeFilter}
          onChange={handleProviderTypeChange}
          data-testid="provider-type-filter"
          aria-label="Filter by provider type"
        >
          <Option value="INTERNAL">INTERNAL</Option>
        </Select>
      </Space>

      {isLoading && (
        <div style={{ textAlign: 'center', padding: 48 }} data-testid="account-list-loading">
          <Spin size="large" />
        </div>
      )}

      {!isLoading && data && (
        <Table<AccountSummaryDto>
          dataSource={data.items}
          columns={columns}
          rowKey="accountId"
          data-testid="account-list-table"
          locale={{ emptyText: 'No accounts found.' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: data.totalCount,
            onChange: (p) => setPage(p),
            showTotal: (total) => `Total ${total} accounts`,
          }}
        />
      )}
    </div>
  );
};

export default AccountManagementPage;
