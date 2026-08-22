import React, { useState } from 'react';
import { Alert, Button, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { searchGraves, getGraveCompanyLookups, getGraveZoneLookups } from './gravesApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import {
  GRAVE_CAPACITY_FILTER,
  GRAVE_STATUS_COLORS,
  GRAVE_STATUSES,
  GRAVE_TYPES,
  GRAVE_TYPE_FILTER,
  type GraveListItem,
} from './types';
import { listTags } from '../tags/tagsApi';
import TagChips from '../tags/TagChips';

const { Title } = Typography;

const GravesPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [zone, setZone] = useState<string | undefined>(undefined);
  const [status, setStatus] = useState<string | undefined>(undefined);
  const [graveType, setGraveType] = useState<string | undefined>(undefined);
  const [companyFilter, setCompanyFilter] = useState<number | undefined>(undefined);
  const [capacityFilter, setCapacityFilter] = useState<string | undefined>(undefined);
  const [tagFilter, setTagFilter] = useState<number[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const { data, isLoading, error } = useQuery({
    queryKey: ['graves', search, zone, status, graveType, companyFilter, capacityFilter, tagFilter, page, pageSize],
    queryFn: () => searchGraves({ search, zone, status, graveType, companyId: companyFilter, capacity: capacityFilter, tagIds: tagFilter, page, pageSize }),
  });

  const { data: tagOptions } = useQuery({
    queryKey: ['tags', 'GRAVE'],
    queryFn: () => listTags('GRAVE'),
    staleTime: 5 * 60 * 1000,
  });

  const { data: companyOptions } = useQuery({
    queryKey: ['grave-company-lookups'],
    queryFn: getGraveCompanyLookups,
    staleTime: 5 * 60 * 1000,
  });

  // Khu ăn theo CÔNG TY đã chọn: chỉ nạp khi có công ty; đổi công ty thì bỏ chọn khu cũ.
  const { data: zoneOptions } = useQuery({
    queryKey: ['grave-zone-lookups', companyFilter],
    queryFn: () => getGraveZoneLookups(companyFilter!),
    enabled: companyFilter != null,
    staleTime: 5 * 60 * 1000,
  });

  const handleCompanyChange = (val: number | undefined) => {
    setCompanyFilter(val);
    setZone(undefined);
    setPage(1);
  };

  if (isPermissionDenied(error)) {
    return <Alert type="error" message="Bạn không có quyền xem danh sách mộ." data-testid="permission-denied" />;
  }

  const columns = [
    { title: 'Mã mộ', dataIndex: 'graveCode', key: 'graveCode' },
    { title: 'Khu', dataIndex: 'zone', key: 'zone', render: (z: string) => `Khu ${z}` },
    { title: 'Số mộ', dataIndex: 'plotNumber', key: 'plotNumber' },
    {
      title: 'Loại',
      dataIndex: 'graveType',
      key: 'graveType',
      render: (t: string) => GRAVE_TYPES[t] ?? t,
    },
    {
      title: 'Diện tích',
      dataIndex: 'areaM2',
      key: 'areaM2',
      render: (a: number | null) => (a != null ? `${a} m²` : '—'),
    },
    {
      title: 'Số cốt',
      dataIndex: 'cotCount',
      key: 'cotCount',
    },
    {
      title: 'Chủ mộ',
      dataIndex: 'ownerName',
      key: 'ownerName',
      render: (n: string | null) => n ?? '—',
    },
    {
      title: 'Công ty',
      dataIndex: 'companyName',
      key: 'companyName',
      render: (n: string | null) => n ?? '—',
    },
    {
      title: 'Người an táng',
      dataIndex: 'occupantCount',
      key: 'occupantCount',
      render: (c: number, r: GraveListItem) => (
        <span style={c > r.cotCount ? { color: '#cf1322', fontWeight: 600 } : undefined}>
          {c}
        </span>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (s: string) => <Tag color={GRAVE_STATUS_COLORS[s] ?? 'default'}>{GRAVE_STATUSES[s] ?? s}</Tag>,
    },
    {
      title: 'Thẻ',
      key: 'tags',
      render: (_: unknown, r: GraveListItem) => <TagChips tags={r.tags} size="small" />,
    },
  ];

  return (
    <div data-testid="graves-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Quản lý mộ</Title>
        {hasPermission('GRAVE_CREATE') && (
          <Button type="primary" data-testid="create-grave-btn">
            <Link to="/graves/new">Thêm mộ</Link>
          </Button>
        )}
      </Space>

      {/* Thứ tự lọc theo phễu: Tìm kiếm → Công ty → Khu (ăn theo công ty) → Loại mộ → Trạng thái → Sức chứa → Thẻ. */}
      <Space style={{ marginBottom: 16 }} wrap>
        <Input.Search
          placeholder="Tìm mã mộ, số mộ, tên người an táng, chủ mộ..."
          allowClear
          onSearch={(val) => { setSearch(val); setPage(1); }}
          style={{ width: 340 }}
          data-testid="grave-search"
        />
        <Select
          placeholder="Công ty"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ minWidth: 200 }}
          value={companyFilter}
          onChange={handleCompanyChange}
          data-testid="grave-company-filter"
          options={(companyOptions ?? []).map((c) => ({ label: c.name, value: c.id }))}
        />
        <Select
          placeholder={companyFilter == null ? 'Khu (chọn công ty trước)' : 'Khu'}
          allowClear
          showSearch
          optionFilterProp="label"
          disabled={companyFilter == null}
          style={{ width: 190 }}
          value={zone}
          onChange={(val) => { setZone(val); setPage(1); }}
          data-testid="grave-zone-filter"
          options={(zoneOptions ?? []).map((z) => ({ label: `Khu ${z}`, value: z }))}
        />
        <Select
          placeholder="Loại mộ"
          allowClear
          style={{ width: 170 }}
          value={graveType}
          onChange={(val) => { setGraveType(val); setPage(1); }}
          options={Object.entries(GRAVE_TYPE_FILTER).map(([value, label]) => ({ label, value }))}
        />
        <Select
          placeholder="Trạng thái"
          allowClear
          style={{ width: 150 }}
          value={status}
          onChange={(val) => { setStatus(val); setPage(1); }}
          options={Object.entries(GRAVE_STATUSES).map(([value, label]) => ({ label, value }))}
        />
        <Select
          placeholder="Sức chứa (người an táng / số cốt)"
          allowClear
          style={{ width: 250 }}
          value={capacityFilter}
          onChange={(val) => { setCapacityFilter(val); setPage(1); }}
          data-testid="grave-capacity-filter"
          options={Object.entries(GRAVE_CAPACITY_FILTER).map(([value, label]) => ({ label, value }))}
        />
        <Select
          mode="multiple"
          placeholder="Lọc theo thẻ"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ minWidth: 200 }}
          value={tagFilter}
          onChange={(val: number[]) => { setTagFilter(val); setPage(1); }}
          data-testid="grave-tag-filter"
          options={(tagOptions ?? []).map((t) => ({ label: `#${t.name}`, value: t.id }))}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert type="error" message={getErrorMessage(error)} style={{ marginBottom: 16 }} data-testid="grave-list-error" />
      )}

      {isLoading && <Spin data-testid="grave-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert type="info" message="Không tìm thấy phần mộ nào." data-testid="grave-list-empty" />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="grave-list-table"
          onRow={(record: GraveListItem) => ({
            onClick: () => navigate(`/graves/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          pagination={{
            current: data.page,
            pageSize: data.pageSize,
            total: data.totalCount,
            showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} mộ`,
            onChange: (p, ps) => { setPage(p); setPageSize(ps); },
          }}
        />
      )}
    </div>
  );
};

export default GravesPage;
