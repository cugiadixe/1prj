import React from 'react';
import { Card, Col, Row, Spin, Tag, Typography } from 'antd';
import {
  HomeOutlined,
  HeartOutlined,
  ContainerOutlined,
  GiftOutlined,
  EnvironmentOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getCustomerOverview } from './customersApi';
import { listByCustomer } from '../customerCarePackages/api';

const { Text } = Typography;

interface Props {
  customerId: number;
  customerStatus: string;
}

const Tile: React.FC<{ icon: React.ReactNode; label: string; children: React.ReactNode; testId?: string }> = ({
  icon, label, children, testId,
}) => (
  <Col xs={12} sm={8} md={6} lg={4}>
    <Card size="small" styles={{ body: { padding: '10px 14px' } }} data-testid={testId}>
      <div style={{ color: '#64748b', fontSize: 12, marginBottom: 4 }}>
        {icon} {label}
      </div>
      <div style={{ fontSize: 20, fontWeight: 600, lineHeight: 1.2 }}>{children}</div>
    </Card>
  </Col>
);

/**
 * Dải tổng quan 360 của khách: tình trạng · số mộ sở hữu · tổng cốt (đang an táng / sức chứa) ·
 * gói chăm sóc hiệu lực · nơi an táng. Dùng chung query ['customer-overview', id] và ['ccp', id]
 * với các section bên dưới nên không phát sinh request thừa.
 */
const Customer360Summary: React.FC<Props> = ({ customerId, customerStatus }) => {
  const { hasPermission } = usePermissions();
  const canViewCcp = hasPermission('CUSTOMER_CARE_PACKAGE_VIEW');

  const { data: overview, isLoading } = useQuery({
    queryKey: ['customer-overview', customerId],
    queryFn: () => getCustomerOverview(customerId),
    enabled: !Number.isNaN(customerId),
  });

  const { data: packages } = useQuery({
    queryKey: ['ccp', customerId],
    queryFn: () => listByCustomer(customerId),
    enabled: !Number.isNaN(customerId) && canViewCcp,
  });

  const isDeceased = customerStatus === 'DECEASED';
  const ownedGraves = overview?.ownedGraves ?? [];
  const buried = (overview?.buriedIn ?? []).filter((b) => b.occupantStatus === 'ACTIVE');
  const activeCot = ownedGraves.reduce((s, g) => s + g.activeOccupantCount, 0);
  const capacity = ownedGraves.reduce((s, g) => s + g.cotCount, 0);
  const activePackages = (packages ?? []).filter((p) => p.status === 'ACTIVE').length;

  return (
    <Card
      size="small"
      style={{ marginBottom: 16, background: '#fafafa' }}
      styles={{ body: { padding: 12 } }}
      data-testid="customer-360-summary"
    >
      {isLoading && <Spin size="small" style={{ marginBottom: 8 }} />}
      <Row gutter={[12, 12]}>
        <Tile icon={<HeartOutlined />} label="Tình trạng" testId="tile-life">
          <Tag color={isDeceased ? 'volcano' : 'green'}>{isDeceased ? 'Đã mất' : 'Còn sống'}</Tag>
        </Tile>
        <Tile icon={<HomeOutlined />} label="Phần mộ sở hữu" testId="tile-owned">
          {ownedGraves.length} <span style={{ fontSize: 13, fontWeight: 400, color: '#64748b' }}>mộ</span>
        </Tile>
        <Tile icon={<ContainerOutlined />} label="Cốt đang an táng" testId="tile-cot">
          {activeCot}
          <span style={{ fontSize: 13, fontWeight: 400, color: '#64748b' }}> / {capacity} chỗ</span>
        </Tile>
        {canViewCcp && (
          <Tile icon={<GiftOutlined />} label="Gói CS hiệu lực" testId="tile-ccp">
            {activePackages} <span style={{ fontSize: 13, fontWeight: 400, color: '#64748b' }}>gói</span>
          </Tile>
        )}
        {buried.length > 0 && (
          <Tile icon={<EnvironmentOutlined />} label="Được an táng tại" testId="tile-buried">
            {buried.map((b) => (
              <div key={b.graveId} style={{ fontSize: 15 }}>
                <Link to={`/graves/${b.graveId}`}>{b.graveCode}</Link>
              </div>
            ))}
          </Tile>
        )}
      </Row>
      {overview?.graveAccessDenied && (
        <Text type="secondary" style={{ fontSize: 12 }}>
          * Bạn không có quyền xem dữ liệu phần mộ — các chỉ số mộ/cốt để trống.
        </Text>
      )}
    </Card>
  );
};

export default Customer360Summary;
