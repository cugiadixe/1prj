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
  const { companies } = useCompany();
  const navigate = useNavigate();

  const [companyFilter, setCompanyFilter] = useState<number | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);
  const [customerIdFilter, setCustomerIdFilter] = useState<number | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const hasViewPerm = hasPermission('SERVICE_VIEW');

  const { data, isLoading, error } = useQuery({
    queryKey: ['services', companyFilter, customerIdFilter, statusFilter, page, pageSize],
    queryFn: () => searchServices({
      companyId: companyFilter,
      customerId: customerIdFilter,
      status: statusFilter,
      page,
      pageSize,
    }),
    enabled: hasViewPerm,
  });

  if (!hasViewPerm || isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem dịch vụ cho công ty này."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'Loại dịch vụ',
      dataIndex: 'serviceTypeName',
      key: 'serviceTypeName',
      render: (val: string | null, record: ServiceListItem) => val || record.serviceTypeCode || String(record.serviceTypeId),
    },
    {
      title: 'Khách hàng',
      key: 'customer',
      render: (_: unknown, r: ServiceListItem) =>
        r.customerName ? `${r.customerName} (${r.customerCode ?? r.customerId})` : (r.customerCode ?? String(r.customerId)),
    },
    {
      title: 'Công ty',
      dataIndex: 'companyName',
      key: 'companyName',
      render: (v: string | null, r: ServiceListItem) => v ?? `Mã ${r.companyId}`,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const map: Record<string, { c: string; t: string }> = {
          ACTIVE: { c: 'green', t: 'Hoạt động' },
          EXPIRED: { c: 'orange', t: 'Hết hạn' },
          CANCELLED: { c: 'red', t: 'Đã hủy' },
          PENDING_PRICE_OVERRIDE: { c: 'gold', t: 'Chờ duyệt giá' },
        };
        const m = map[status] ?? { c: 'default', t: status };
        return <Tag color={m.c}>{m.t}</Tag>;
      },
    },
    {
      title: 'Giá áp dụng',
      dataIndex: 'appliedPrice',
      key: 'appliedPrice',
      render: (val: number, record: ServiceListItem) => (
        <Space>
          {val.toLocaleString('vi-VN')}
          {record.isOverridePrice && <Tag color="blue" style={{ marginLeft: 8 }}>OVERRIDE</Tag>}
        </Space>
      ),
    },
    {
      title: 'Hiệu lực từ',
      dataIndex: 'validFrom',
      key: 'validFrom',
      render: (val: string) => new Date(val).toLocaleDateString('vi-VN'),
    },
    {
      title: 'Hiệu lực đến',
      dataIndex: 'validTo',
      key: 'validTo',
      render: (val: string | null) => val ? new Date(val).toLocaleDateString('vi-VN') : '—',
    },
  ];

  return (
    <div data-testid="services-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Bảng tổng hợp dịch vụ</Title>
        <Space>
          {hasPermission('SERVICE_CREATE_STANDARD') && (
            <Button type="primary" data-testid="create-service-btn">
              <Link to="/services/new">Tạo dịch vụ</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          placeholder="Tất cả công ty"
          allowClear
          style={{ width: 200 }}
          onChange={(val) => { setCompanyFilter(val); setPage(1); }}
          value={companyFilter}
          data-testid="service-company-filter"
          options={companies.map((c) => ({ label: c.companyName, value: c.companyId }))}
        />
        <Input
          placeholder="Mã KH (số)"
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
          placeholder="Trạng thái"
          allowClear
          style={{ width: 150 }}
          onChange={(val) => { setStatusFilter(val); setPage(1); }}
          value={statusFilter}
          data-testid="service-status-filter"
          options={[
            { label: 'Hoạt động', value: 'ACTIVE' },
            { label: 'Hết hạn', value: 'EXPIRED' },
            { label: 'Đã hủy', value: 'CANCELLED' },
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
          message="Không tìm thấy dịch vụ."
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
