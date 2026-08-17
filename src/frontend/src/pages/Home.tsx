import React from 'react';
import { Card, Col, Row, Spin, Typography, Alert } from 'antd';
import {
  TeamOutlined,
  BankOutlined,
  HeartOutlined,
  DollarOutlined,
  EnvironmentOutlined,
  ClockCircleOutlined,
  PieChartOutlined,
  BarChartOutlined,
  AreaChartOutlined,
  AppstoreOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { getDashboardSummary } from '../dashboard/dashboardApi';
import { AreaChart, BarChart, DonutChart, HBarChart, PALETTE, fmtCompactVnd } from '../dashboard/Charts';
import {
  carePackageStatusData,
  customerStatusData,
  graveStatusData,
  graveTypeData,
  monthLabel,
  serviceStatusData,
  zoneData,
} from '../dashboard/labels';

const { Title, Text } = Typography;

const Home: React.FC = () => {
  const { user } = useAuth();
  const { currentCompanyId } = useCompany();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['dashboard-summary', currentCompanyId],
    queryFn: () => getDashboardSummary(currentCompanyId!),
    enabled: !!currentCompanyId,
  });

  const now = new Date();
  const hour = now.getHours();
  const greeting = hour < 12 ? 'Chào buổi sáng' : hour < 18 ? 'Chào buổi chiều' : 'Chào buổi tối';

  const occupancy = data && data.totalGraves > 0 ? Math.round((data.occupiedGraves / data.totalGraves) * 100) : 0;

  const kpis = [
    { title: 'Khách hàng', value: data ? data.totalCustomers.toLocaleString('vi-VN') : '—', icon: <TeamOutlined />, color: '#3b82f6', bg: '#eff6ff' },
    { title: 'Phần mộ', value: data ? data.totalGraves.toLocaleString('vi-VN') : '—', icon: <EnvironmentOutlined />, color: '#8b5cf6', bg: '#f5f3ff' },
    { title: 'Tỉ lệ lấp đầy', value: data ? `${occupancy}%` : '—', icon: <BankOutlined />, color: '#06b6d4', bg: '#ecfeff' },
    { title: 'Doanh thu', value: data ? fmtCompactVnd(data.totalRevenue) : '—', icon: <DollarOutlined />, color: '#22c55e', bg: '#f0fdf4' },
    { title: 'Gói CS hiệu lực', value: data ? data.activeCarePackages.toLocaleString('vi-VN') : '—', icon: <HeartOutlined />, color: '#ec4899', bg: '#fdf2f8' },
  ];

  const cardHead = (icon: React.ReactNode, text: string) => (
    <span>{icon} <span style={{ marginLeft: 6 }}>{text}</span></span>
  );

  return (
    <div>
      <div style={{ marginBottom: 20 }}>
        <Title level={4} style={{ marginBottom: 2 }}>
          {greeting}, {user?.displayName || user?.username || 'bạn'}
        </Title>
        <Text type="secondary" style={{ fontSize: 13 }}>
          <ClockCircleOutlined style={{ marginRight: 4 }} />
          {now.toLocaleDateString('vi-VN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
        </Text>
      </div>

      {!currentCompanyId && (
        <Alert type="info" showIcon message="Hãy chọn công ty để xem số liệu tổng quan." style={{ marginBottom: 16 }} />
      )}
      {isError && (
        <Alert type="error" showIcon message="Không tải được số liệu tổng quan." style={{ marginBottom: 16 }} />
      )}

      {/* KPI */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 4, flexWrap: 'wrap' }}>
        {kpis.map((k) => (
          <div key={k.title} style={{ flex: '1 1 180px', minWidth: 170, background: '#fff', borderRadius: 10, padding: '14px 18px', borderLeft: `4px solid ${k.color}`, display: 'flex', alignItems: 'center', gap: 12, boxShadow: '0 1px 3px rgba(0,0,0,0.06)' }}>
            <div style={{ width: 42, height: 42, borderRadius: 10, background: k.bg, color: k.color, fontSize: 20, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
              {k.icon}
            </div>
            <div style={{ minWidth: 0 }}>
              <div style={{ fontSize: 12, color: '#64748b', whiteSpace: 'nowrap' }}>{k.title}</div>
              <div style={{ fontSize: 20, fontWeight: 700, color: '#1e293b', lineHeight: 1.3 }}>{k.value}</div>
            </div>
          </div>
        ))}
      </div>

      <Spin spinning={isLoading}>
        <Row gutter={[16, 16]} style={{ marginTop: 12 }}>
          <Col xs={24} lg={12}>
            <Card size="small" title={cardHead(<AreaChartOutlined style={{ color: PALETTE[1] }} />, 'Doanh thu 6 tháng gần nhất')}>
              <AreaChart
                data={(data?.revenueByMonth ?? []).map((r) => ({ label: monthLabel(r.month), value: r.amount }))}
                color={PALETTE[1]}
                valueFormatter={fmtCompactVnd}
              />
            </Card>
          </Col>
          <Col xs={24} lg={12}>
            <Card size="small" title={cardHead(<AreaChartOutlined style={{ color: PALETTE[3] }} />, 'Gói chăm sóc bán theo tháng')}>
              <AreaChart
                data={(data?.carePackagesByMonth ?? []).map((r) => ({ label: monthLabel(r.month), value: r.count }))}
                color={PALETTE[3]}
              />
            </Card>
          </Col>

          <Col xs={24} md={12} lg={8}>
            <Card size="small" title={cardHead(<PieChartOutlined style={{ color: PALETTE[0] }} />, 'Phần mộ theo trạng thái')}>
              <DonutChart data={graveStatusData(data?.gravesByStatus ?? [])} unit="Mộ" />
            </Card>
          </Col>
          <Col xs={24} md={12} lg={8}>
            <Card size="small" title={cardHead(<PieChartOutlined style={{ color: PALETTE[2] }} />, 'Phần mộ theo loại')}>
              <DonutChart data={graveTypeData(data?.gravesByType ?? [])} unit="Mộ" />
            </Card>
          </Col>
          <Col xs={24} md={12} lg={8}>
            <Card size="small" title={cardHead(<PieChartOutlined style={{ color: PALETTE[5] }} />, 'Dịch vụ theo trạng thái')}>
              <DonutChart data={serviceStatusData(data?.servicesByStatus ?? [])} unit="DV" />
            </Card>
          </Col>

          <Col xs={24} lg={12}>
            <Card size="small" title={cardHead(<BarChartOutlined style={{ color: PALETTE[0] }} />, 'Phần mộ theo khu')}>
              <BarChart data={zoneData(data?.gravesByZone ?? [])} color={PALETTE[0]} />
            </Card>
          </Col>
          <Col xs={24} lg={12}>
            <Card size="small" title={cardHead(<BarChartOutlined style={{ color: PALETTE[6] }} />, 'Gói chăm sóc theo trạng thái')}>
              <BarChart data={carePackageStatusData(data?.carePackagesByStatus ?? [])} color={PALETTE[6]} />
            </Card>
          </Col>

          <Col xs={24}>
            <Card size="small" title={cardHead(<AppstoreOutlined style={{ color: PALETTE[4] }} />, 'Khách hàng theo trạng thái')}>
              <HBarChart data={customerStatusData(data?.customersByStatus ?? [])} />
            </Card>
          </Col>
        </Row>
      </Spin>
    </div>
  );
};

export default Home;
