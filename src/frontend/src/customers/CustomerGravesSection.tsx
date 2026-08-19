import React from 'react';
import { Alert, Card, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getCustomerOverview } from './customersApi';
import { GRAVE_STATUSES, GRAVE_STATUS_COLORS, GRAVE_TYPES } from '../graves/types';
import type { BuriedInGrave, OverviewGrave } from './types';

const { Text } = Typography;

const fmtDate = (v: string | null): string => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');

interface Props {
  customerId: number;
}

/**
 * Trục PHẦN MỘ của bảng điều khiển 360: phần mộ khách SỞ HỮU (kèm số cốt/sức chứa) và nơi khách
 * ĐƯỢC AN TÁNG (là cốt). Dùng chung query ['customer-overview', id] với dải tổng quan.
 */
const CustomerGravesSection: React.FC<Props> = ({ customerId }) => {
  const { data: overview, isLoading } = useQuery({
    queryKey: ['customer-overview', customerId],
    queryFn: () => getCustomerOverview(customerId),
    enabled: !Number.isNaN(customerId),
  });

  const ownedColumns = [
    {
      title: 'Mã mộ',
      key: 'graveCode',
      render: (_: unknown, r: OverviewGrave) => <Link to={`/graves/${r.graveId}`}>{r.graveCode}</Link>,
    },
    { title: 'Nghĩa trang', dataIndex: 'cemeteryName', key: 'cemeteryName', render: (v: string | null) => v ?? '—' },
    {
      title: 'Khu / Ô',
      key: 'zone',
      render: (_: unknown, r: OverviewGrave) => `${r.zone} / ${r.plotNumber}`,
    },
    { title: 'Loại', dataIndex: 'graveType', key: 'graveType', render: (v: string) => GRAVE_TYPES[v] ?? v },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (s: string) => <Tag color={GRAVE_STATUS_COLORS[s] ?? 'default'}>{GRAVE_STATUSES[s] ?? s}</Tag>,
    },
    {
      title: 'Số cốt',
      key: 'cot',
      render: (_: unknown, r: OverviewGrave) => (
        <span>
          <b>{r.activeOccupantCount}</b>
          <Text type="secondary"> / {r.cotCount} chỗ</Text>
        </span>
      ),
    },
  ];

  const buriedColumns = [
    {
      title: 'Mã mộ',
      key: 'graveCode',
      render: (_: unknown, r: BuriedInGrave) => <Link to={`/graves/${r.graveId}`}>{r.graveCode}</Link>,
    },
    {
      title: 'Nghĩa trang / Khu',
      key: 'cemetery',
      render: (_: unknown, r: BuriedInGrave) => `${r.cemeteryName ?? '—'} / Khu ${r.zone}`,
    },
    { title: 'Ngày an táng', key: 'burialDate', render: (_: unknown, r: BuriedInGrave) => fmtDate(r.burialDate) },
    {
      title: 'Quan hệ với chủ mộ',
      dataIndex: 'deceasedRelationship',
      key: 'rel',
      render: (v: string | null) => v ?? '—',
    },
    {
      title: 'Chủ mộ',
      key: 'owner',
      render: (_: unknown, r: BuriedInGrave) =>
        r.ownerCustomerId ? <Link to={`/customers/${r.ownerCustomerId}`}>{r.ownerName ?? `#${r.ownerCustomerId}`}</Link> : '—',
    },
    {
      title: 'Trạng thái suất',
      key: 'occStatus',
      render: (_: unknown, r: BuriedInGrave) =>
        r.occupantStatus === 'ACTIVE' ? (
          <Tag color="green">Đang an táng</Tag>
        ) : (
          <Tag color="orange">Đã bốc{r.relocatedAt ? ` (${fmtDate(r.relocatedAt)})` : ''}</Tag>
        ),
    },
  ];

  if (isLoading) {
    return (
      <Card title="Phần mộ" style={{ marginBottom: 16 }} data-testid="customer-graves-card">
        <Spin />
      </Card>
    );
  }

  const owned = overview?.ownedGraves ?? [];
  const buried = overview?.buriedIn ?? [];

  return (
    <>
      <Card title="Phần mộ sở hữu (chủ mộ)" style={{ marginBottom: 16 }} data-testid="customer-owned-graves-card">
        {overview?.graveAccessDenied ? (
          <Alert type="warning" showIcon message="Bạn không có quyền xem dữ liệu phần mộ." />
        ) : owned.length === 0 ? (
          <Alert type="info" message="Khách hàng chưa sở hữu phần mộ nào." data-testid="no-owned-graves" />
        ) : (
          <Table
            columns={ownedColumns}
            dataSource={owned}
            rowKey="graveId"
            pagination={false}
            size="small"
            data-testid="owned-graves-table"
          />
        )}
      </Card>

      {buried.length > 0 && (
        <Card title="Được an táng tại" style={{ marginBottom: 16 }} data-testid="customer-buried-in-card">
          <Space direction="vertical" size={8} style={{ width: '100%' }}>
            <Text type="secondary">Khách hàng này là cốt trong (các) phần mộ dưới đây.</Text>
            <Table
              columns={buriedColumns}
              dataSource={buried}
              rowKey="graveId"
              pagination={false}
              size="small"
              data-testid="buried-in-table"
            />
          </Space>
        </Card>
      )}
    </>
  );
};

export default CustomerGravesSection;
