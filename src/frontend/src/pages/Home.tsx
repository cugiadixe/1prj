import React, { useEffect, useState } from 'react';
import { Card, Typography, Spin, Table, Tag, Empty } from 'antd';
import {
  TeamOutlined,
  DollarOutlined,
  ToolOutlined,
  CheckSquareOutlined,
  BankOutlined,
  ArrowRightOutlined,
  ClockCircleOutlined,
  CreditCardOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { useAuth } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';

const { Title, Text } = Typography;

interface DashboardStats {
  customers: number;
  payments: number;
  services: number;
  pendingApprovals: number;
  companies: number;
}

interface RecentPayment {
  key: string;
  billCode: string;
  totalAmount: number;
  status: string;
  paymentDate: string;
}

const STATUS_COLORS: Record<string, string> = {
  DRAFT: 'default',
  PENDING: 'processing',
  APPROVED: 'success',
  COMPLETED: 'success',
  REJECTED: 'error',
  CANCELLED: 'error',
};

const Home: React.FC = () => {
  const { user } = useAuth();
  const { currentCompanyId } = useCompany();
  const navigate = useNavigate();
  const [stats, setStats] = useState<DashboardStats>({
    customers: 0, payments: 0, services: 0, pendingApprovals: 0, companies: 0,
  });
  const [recentPayments, setRecentPayments] = useState<RecentPayment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchStats = async () => {
      setLoading(true);
      try {
        const headers: Record<string, string> = {};
        if (currentCompanyId) {
          headers['X-Company-Id'] = String(currentCompanyId);
        }

        const [customersRes, paymentsRes, servicesRes, approvalsRes, companiesRes] = await Promise.allSettled([
          axiosClient.get('/customers', { params: { pageSize: 1 }, headers }),
          axiosClient.get('/payments', { params: { pageSize: 5 }, headers }),
          axiosClient.get('/services', { params: { pageSize: 1 }, headers }),
          axiosClient.get('/workflow/my-approvals', { params: { pageSize: 1 }, headers }),
          axiosClient.get('/companies', { headers }),
        ]);

        const getTotal = (res: PromiseSettledResult<any>) =>
          res.status === 'fulfilled' ? (res.value.data?.totalCount ?? res.value.data?.items?.length ?? 0) : 0;

        const paymentsData = paymentsRes.status === 'fulfilled' ? paymentsRes.value.data : null;

        setStats({
          customers: getTotal(customersRes),
          payments: getTotal(paymentsRes),
          services: getTotal(servicesRes),
          pendingApprovals: getTotal(approvalsRes),
          companies: companiesRes.status === 'fulfilled' ? (companiesRes.value.data?.length ?? companiesRes.value.data?.items?.length ?? 0) : 0,
        });

        if (paymentsData?.items) {
          setRecentPayments(paymentsData.items.slice(0, 5).map((p: any) => ({
            key: p.id ?? p.billCode,
            billCode: p.billCode,
            totalAmount: p.totalAmount,
            status: p.status,
            paymentDate: p.paymentDate ? new Date(p.paymentDate).toLocaleDateString('vi-VN') : '',
          })));
        }
      } catch {
        // silently fail
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, [currentCompanyId]);

  const statCards = [
    { title: 'Khách hàng', value: stats.customers, icon: <TeamOutlined />, color: '#1890ff', bg: '#e6f4ff', path: '/customers' },
    { title: 'Thanh toán', value: stats.payments, icon: <DollarOutlined />, color: '#52c41a', bg: '#f6ffed', path: '/payments' },
    { title: 'Dịch vụ', value: stats.services, icon: <ToolOutlined />, color: '#722ed1', bg: '#f9f0ff', path: '/services' },
    { title: 'Chờ duyệt', value: stats.pendingApprovals, icon: <CheckSquareOutlined />, color: '#faad14', bg: '#fffbe6', path: '/workflow/my-approvals' },
    { title: 'Công ty', value: stats.companies, icon: <BankOutlined />, color: '#13c2c2', bg: '#e6fffb', path: undefined },
  ];

  const paymentColumns = [
    { title: 'Mã hóa đơn', dataIndex: 'billCode', key: 'billCode' },
    {
      title: 'Số tiền',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (v: number) => v ? <Text strong>{v.toLocaleString('vi-VN')} VND</Text> : '',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (s: string) => <Tag color={STATUS_COLORS[s] || 'default'}>{s}</Tag>,
    },
    { title: 'Ngày', dataIndex: 'paymentDate', key: 'paymentDate' },
  ];

  const quickLinks = [
    { title: 'Tạo khách hàng mới', path: '/customers/proposals/create', icon: <TeamOutlined /> },
    { title: 'Tra cứu dịch vụ', path: '/services', icon: <ToolOutlined /> },
    { title: 'Yêu cầu chờ duyệt', path: '/workflow/my-approvals', icon: <CheckSquareOutlined /> },
    { title: 'In lại thẻ', path: '/cards/reprints', icon: <CreditCardOutlined /> },
  ];

  const now = new Date();
  const hour = now.getHours();
  const greeting = hour < 12 ? 'Chào buổi sáng' : hour < 18 ? 'Chào buổi chiều' : 'Chào buổi tối';

  return (
    <Spin spinning={loading}>
      <div style={{ marginBottom: 20 }}>
        <Title level={4} style={{ marginBottom: 2 }}>
          {greeting}, {user?.displayName || user?.username || 'User'}
        </Title>
        <Text type="secondary" style={{ fontSize: 13 }}>
          <ClockCircleOutlined style={{ marginRight: 4 }} />
          {now.toLocaleDateString('vi-VN', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
        </Text>
      </div>

      <div style={{ display: 'flex', gap: 12, marginBottom: 20, flexWrap: 'wrap' }}>
        {statCards.map((card) => (
          <div
            key={card.title}
            onClick={() => card.path && navigate(card.path)}
            style={{
              flex: '1 1 0',
              minWidth: 180,
              background: '#fff',
              borderRadius: 8,
              padding: '16px 20px',
              cursor: card.path ? 'pointer' : 'default',
              borderLeft: `4px solid ${card.color}`,
              display: 'flex',
              alignItems: 'center',
              gap: 14,
              boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
              transition: 'box-shadow 0.2s',
            }}
            onMouseEnter={(e) => { if (card.path) e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.12)'; }}
            onMouseLeave={(e) => { e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.06)'; }}
          >
            <div style={{
              width: 44, height: 44, borderRadius: 10,
              background: card.bg,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: card.color, fontSize: 22, flexShrink: 0,
            }}>
              {card.icon}
            </div>
            <div>
              <div style={{ fontSize: 12, color: '#888', whiteSpace: 'nowrap' }}>{card.title}</div>
              <div style={{ fontSize: 22, fontWeight: 700, lineHeight: 1.3, color: '#222' }}>
                {(card.value ?? 0).toLocaleString('vi-VN')}
              </div>
            </div>
          </div>
        ))}
      </div>

      <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start', flexWrap: 'wrap' }}>
        <div style={{ flex: '2 1 500px', minWidth: 0 }}>
          <Card
            title={<><DollarOutlined style={{ marginRight: 8 }} />Giao dịch gần đây</>}
            extra={<a onClick={() => navigate('/payments')}>Xem tất cả <ArrowRightOutlined /></a>}
            styles={{ body: { padding: recentPayments.length > 0 ? undefined : '16px 24px' } }}
          >
            {recentPayments.length > 0 ? (
              <Table
                dataSource={recentPayments}
                columns={paymentColumns}
                pagination={false}
                size="middle"
              />
            ) : (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description="Chưa có giao dịch nào"
                style={{ padding: '32px 0' }}
              />
            )}
          </Card>
        </div>

        <div style={{ flex: '1 1 280px', minWidth: 280 }}>
          <Card title={<><ArrowRightOutlined style={{ marginRight: 8 }} />Truy cập nhanh</>}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {quickLinks.map((link) => (
                <div
                  key={link.path}
                  onClick={() => navigate(link.path)}
                  style={{
                    display: 'flex', alignItems: 'center', gap: 10,
                    padding: '10px 14px', borderRadius: 6,
                    border: '1px solid #f0f0f0', cursor: 'pointer',
                    transition: 'background 0.15s',
                  }}
                  onMouseEnter={(e) => { e.currentTarget.style.background = '#f5f5f5'; }}
                  onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent'; }}
                >
                  <span style={{ fontSize: 16, color: '#1890ff' }}>{link.icon}</span>
                  <span>{link.title}</span>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>
    </Spin>
  );
};

export default Home;
