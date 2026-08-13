import React from 'react';
import { Alert, Button, Card, Descriptions, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getCustomerById, getCompanyContexts } from './customersApi';
import EntityTagsSection from '../tags/EntityTagsSection';
import { setCustomerTags } from '../tags/tagsApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import { CUSTOMER_STATUS_COLORS, CUSTOMER_STATUS_LABELS, type CustomerCompanyContext } from './types';

const fmtDate = (v: string | null | undefined): string => {
  if (!v) return '—';
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return v;
  const p = (n: number) => String(n).padStart(2, '0');
  return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()}`;
};

const genderLabel = (g: string | null | undefined): string =>
  g === 'MALE' ? 'Nam' : g === 'FEMALE' ? 'Nữ' : g === 'OTHER' ? 'Khác' : (g ?? '—');
import CustomerMasterChangeRequestForm from './CustomerMasterChangeRequestForm';
import CustomerCarePackagesSection from '../customerCarePackages/CustomerCarePackagesSection';

const { Title } = Typography;

const CustomerDetailPage: React.FC = () => {
  const { customerId } = useParams<{ customerId: string }>();
  const { hasPermission } = usePermissions();
  const queryClient = useQueryClient();
  const id = Number(customerId);
  const [showChangeForm, setShowChangeForm] = React.useState(false);
  const {
    data: customer,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['customer', id],
    queryFn: () => getCustomerById(id),
    enabled: !isNaN(id),
  });

  const {
    data: contexts,
    isLoading: contextsLoading,
  } = useQuery({
    queryKey: ['customer-contexts', id],
    queryFn: () => getCompanyContexts(id),
    enabled: !isNaN(id),
  });

  if (isLoading) return <Spin data-testid="customer-detail-loading" />;

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem khách hàng này."
        data-testid="permission-denied"
      />
    );
  }

  if (error) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(error)}
        data-testid="customer-detail-error"
      />
    );
  }

  if (!customer) return null;

  if (showChangeForm) {
    return (
      <CustomerMasterChangeRequestForm
        customerId={id}
        customerName={customer.profile.fullName}
        targetRowVersion={customer.rowVersion}
        profile={customer.profile}
        onCancel={() => setShowChangeForm(false)}
      />
    );
  }

  const profile = customer.profile;
  const isMasked = (val: string | null) =>
    val != null && (val.includes('***') || val.includes('****'));

  const contextColumns = [
    {
      title: 'Công ty phụ trách',
      key: 'company',
      render: (_: unknown, r: CustomerCompanyContext) => r.companyName ?? `Mã ${r.companyId}`,
    },
    {
      title: 'Nhân viên phụ trách',
      key: 'staff',
      render: (_: unknown, r: CustomerCompanyContext) => r.assignedStaffName ?? '— (chưa phân)',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'relationshipStatus',
      key: 'relationshipStatus',
      render: (s: string) => (
        <Tag color={s === 'ACTIVE' ? 'green' : 'default'}>
          {s === 'ACTIVE' ? 'Đang phụ trách' : s === 'INACTIVE' ? 'Ngừng' : s}
        </Tag>
      ),
    },
    { title: 'Ghi chú', dataIndex: 'internalNotes', key: 'internalNotes', render: (v: string | null) => v ?? '—' },
    ...(hasPermission('CUSTOMER_MASTER_UPDATE', 'GLOBAL')
      ? [
          {
            title: 'Thao tác',
            key: 'action',
            render: (_: unknown, record: CustomerCompanyContext) => (
              <Link
                to={`/customers/${id}/edit`}
                state={{ editContext: record }}
                data-testid={`edit-context-${record.id}`}
              >
                Sửa
              </Link>
            ),
          },
        ]
      : []),
  ];

  return (
    <div data-testid="customer-detail-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Khách hàng: {customer.customerCode}
        </Title>
        <Space>
          <Button>
            <Link to="/customers">Quay lại danh sách</Link>
          </Button>
          {hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL') && (
            <Button data-testid="request-change-btn" onClick={() => setShowChangeForm(true)}>
              Yêu cầu thay đổi
            </Button>
          )}
          {hasPermission('CUSTOMER_MASTER_UPDATE', 'GLOBAL') && (
            <Button type="primary" data-testid="edit-customer-btn">
              <Link to={`/customers/${id}/edit`}>Sửa</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Card title="Thông tin khách hàng" style={{ marginBottom: 16 }} data-testid="customer-info-card">
        <Descriptions column={2}>
          <Descriptions.Item label="Mã khách hàng">{customer.customerCode}</Descriptions.Item>
          <Descriptions.Item label="Trạng thái">
            <Tag color={CUSTOMER_STATUS_COLORS[customer.customerStatus] ?? 'default'}>
              {CUSTOMER_STATUS_LABELS[customer.customerStatus] ?? customer.customerStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Ngày tạo">{fmtDate(customer.createdAt)}</Descriptions.Item>
          <Descriptions.Item label="Ngày cập nhật">{fmtDate(customer.updatedAt)}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="Hồ sơ" style={{ marginBottom: 16 }} data-testid="profile-card">
        <Descriptions column={2}>
          <Descriptions.Item label="Họ tên">{profile.fullName}</Descriptions.Item>
          <Descriptions.Item label="Giới tính">{genderLabel(profile.gender)}</Descriptions.Item>
          <Descriptions.Item label="CCCD">
            <span data-testid="profile-cccd">
              {profile.cccd ?? '—'}
              {isMasked(profile.cccd) && <Tag style={{ marginLeft: 4 }}>ẩn</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Điện thoại">
            <span data-testid="profile-phone">
              {profile.phone ?? '—'}
              {isMasked(profile.phone) && <Tag style={{ marginLeft: 4 }}>ẩn</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Địa chỉ thường trú">
            <span data-testid="profile-permanent-address">
              {profile.permanentAddress ?? '—'}
              {isMasked(profile.permanentAddress) && <Tag style={{ marginLeft: 4 }}>ẩn</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Địa chỉ liên hệ">
            <span data-testid="profile-contact-address">
              {profile.contactAddress ?? '—'}
              {isMasked(profile.contactAddress) && <Tag style={{ marginLeft: 4 }}>ẩn</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Ngày sinh">{profile.dob ? fmtDate(profile.dob) : (profile.dobPartial ?? '—')}</Descriptions.Item>
          <Descriptions.Item label="Độ chính xác ngày sinh">{profile.dobPrecision ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Ngày cấp CCCD">{fmtDate(profile.cccdIssueDate)}</Descriptions.Item>
          <Descriptions.Item label="Nơi cấp CCCD">{profile.cccdIssuePlace ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Mã số thuế">{profile.taxCode ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Quê quán">{profile.hometown ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Ngày mất (Dương lịch)">{fmtDate(profile.deathDateSolar)}</Descriptions.Item>
          <Descriptions.Item label="Ngày mất (Âm lịch)">{profile.deathDateLunar ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Nơi mất">{profile.deathPlace ?? '—'}</Descriptions.Item>
        </Descriptions>
      </Card>

      <EntityTagsSection
        tagType="CUSTOMER"
        tags={customer.tags}
        canManage={hasPermission('TAG_MANAGE', 'GLOBAL')}
        onSave={(req) => setCustomerTags(id, req)}
        onSaved={() => queryClient.invalidateQueries({ queryKey: ['customer', id] })}
        testId="customer-tags-section"
      />

      <CustomerCarePackagesSection customerId={id} />

      <Card title="Công ty / nhân viên phụ trách" data-testid="company-contexts-card">
        <Space style={{ marginBottom: 8 }}>
          {hasPermission('CUSTOMER_CREATE_FINAL', 'GLOBAL') && (
            <Button type="primary" size="small" data-testid="add-context-btn" disabled>
              Thêm công ty phụ trách
            </Button>
          )}
        </Space>
        {contextsLoading && <Spin />}
        {contexts && contexts.length === 0 && (
          <Alert type="info" message="Chưa có công ty/nhân viên phụ trách khách hàng này." data-testid="no-contexts" />
        )}
        {contexts && contexts.length > 0 && (
          <Table
            dataSource={contexts}
            columns={contextColumns}
            rowKey="id"
            pagination={false}
            data-testid="contexts-table"
          />
        )}
      </Card>
    </div>
  );
};

export default CustomerDetailPage;
