import React, { useState } from 'react';
import { Alert, Button, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { searchServices } from './servicesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { ServiceListItem } from './types';

const { Title } = Typography;

const ServiceListPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const { currentCompanyId } = useCompany();
  const navigate = useNavigate();

  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [customerIdFilter, setCustomerIdFilter] = useState<number | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const hasViewPerm = hasPermission('SERVICE_VIEW', 'COMPANY');

  const { data, isLoading, error } = useQuery({
    queryKey: ['services', currentCompanyId, customerIdFilter, statusFilter, page, pageSize],
    queryFn: () => searchServices({
      companyId: currentCompanyId!,
      customerId: customerIdFilter,
      status: statusFilter,
      page,
      pageSize,
    }),
    enabled: !!currentCompanyId && hasViewPerm,
  });

  if (!currentCompanyId) {
    return (
      <Alert
        type="warning"
        message="Please select a company to view services."
        data-testid="no-company-warning"
      />
    );
  }

  if (!hasViewPerm || isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="You do not have permission to view services for this company."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Service Type',
      dataIndex: 'serviceTypeName',
      key: 'serviceTypeName',
      render: (val: string | null, record: ServiceListItem) => val || record.serviceTypeCode || String(record.serviceTypeId),
    },
    {
      title: 'Customer ID',
      dataIndex: 'customerId',
      key: 'customerId',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        let color = 'default';
        if (status === 'ACTIVE') color = 'green';
        if (status === 'EXPIRED') color = 'orange';
        if (status === 'CANCELLED') color = 'red';
        return <Tag color={color}>{status}</Tag>;
      },
    },
    {
      title: 'Applied Price',
      dataIndex: 'appliedPrice',
      key: 'appliedPrice',
      render: (val: number, record: ServiceListItem) => (
        <Space>
          {val.toLocaleString()}
          {record.isOverridePrice && <Tag color="blue" style={{ marginLeft: 8 }}>OVERRIDE</Tag>}
        </Space>
      ),
    },
    {
      title: 'Valid From',
      dataIndex: 'validFrom',
      key: 'validFrom',
      render: (val: string) => new Date(val).toLocaleDateString(),
    },
    {
      title: 'Valid To',
      dataIndex: 'validTo',
      key: 'validTo',
      render: (val: string | null) => val ? new Date(val).toLocaleDateString() : '—',
    },
  ];

  return (
    <div data-testid="services-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Services</Title>
        <Space>
          {hasPermission('SERVICE_CREATE_STANDARD', 'COMPANY') && (
            <Button type="primary" data-testid="create-service-btn">
              <Link to="/services/new">Create Service</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Input
          placeholder="Customer ID"
          allowClear
          onChange={(e) => {
            const val = parseInt(e.target.value, 10);
            setCustomerIdFilter(isNaN(val) ? undefined : val);
            setPage(1);
          }}
          style={{ width: 200 }}
          data-testid="service-customer-filter"
        />
        <Select
          placeholder="Status"
          allowClear
          style={{ width: 150 }}
          onChange={(val) => { setStatusFilter(val); setPage(1); }}
          value={statusFilter}
          data-testid="service-status-filter"
          options={[
            { label: 'Active', value: 'ACTIVE' },
            { label: 'Expired', value: 'EXPIRED' },
            { label: 'Cancelled', value: 'CANCELLED' },
          ]}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="service-list-error"
        />
      )}

      {isLoading && <Spin data-testid="service-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="No services found."
          data-testid="service-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="service-list-table"
          onRow={(record: ServiceListItem) => ({
            onClick: () => navigate(`/services/${record.id}`),
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

export default ServiceListPage;
