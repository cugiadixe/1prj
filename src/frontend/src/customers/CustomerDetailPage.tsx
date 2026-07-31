import React from 'react';
import { Alert, Button, Card, Descriptions, Space, Spin, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { getCustomerById, getCompanyContexts } from './customersApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { CustomerCompanyContext } from './types';

const { Title } = Typography;

const CustomerDetailPage: React.FC = () => {
  const { customerId } = useParams<{ customerId: string }>();
  const { hasPermission } = usePermissions();
  const id = Number(customerId);

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
        message="You do not have permission to view this customer."
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

  const profile = customer.profile;
  const isMasked = (val: string | null) =>
    val != null && (val.includes('***') || val.includes('****'));

  const contextColumns = [
    { title: 'Company ID', dataIndex: 'companyId', key: 'companyId' },
    { title: 'Staff ID', dataIndex: 'assignedStaffId', key: 'assignedStaffId', render: (v: number | null) => v ?? '—' },
    {
      title: 'Status',
      dataIndex: 'relationshipStatus',
      key: 'relationshipStatus',
      render: (s: string) => (
        <Tag color={s === 'ACTIVE' ? 'green' : 'red'}>{s}</Tag>
      ),
    },
    { title: 'Notes', dataIndex: 'internalNotes', key: 'internalNotes', render: (v: string | null) => v ?? '—' },
    ...(hasPermission('CUSTOMER_MASTER_UPDATE', 'GLOBAL')
      ? [
          {
            title: 'Action',
            key: 'action',
            render: (_: unknown, record: CustomerCompanyContext) => (
              <Link
                to={`/customers/${id}/edit`}
                state={{ editContext: record }}
                data-testid={`edit-context-${record.id}`}
              >
                Edit
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
          Customer: {customer.customerCode}
        </Title>
        <Space>
          <Button>
            <Link to="/customers">Back to List</Link>
          </Button>
          {hasPermission('CUSTOMER_MASTER_UPDATE', 'GLOBAL') && (
            <Button type="primary" data-testid="edit-customer-btn">
              <Link to={`/customers/${id}/edit`}>Edit</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Card title="Customer Info" style={{ marginBottom: 16 }} data-testid="customer-info-card">
        <Descriptions column={2}>
          <Descriptions.Item label="Customer Code">{customer.customerCode}</Descriptions.Item>
          <Descriptions.Item label="Status">
            <Tag color={customer.customerStatus === 'ACTIVE' ? 'green' : 'red'}>
              {customer.customerStatus}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Created">{customer.createdAt}</Descriptions.Item>
          <Descriptions.Item label="Updated">{customer.updatedAt ?? '—'}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="Profile" style={{ marginBottom: 16 }} data-testid="profile-card">
        <Descriptions column={2}>
          <Descriptions.Item label="Full Name">{profile.fullName}</Descriptions.Item>
          <Descriptions.Item label="Gender">{profile.gender ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="CCCD">
            <span data-testid="profile-cccd">
              {profile.cccd ?? '—'}
              {isMasked(profile.cccd) && <Tag style={{ marginLeft: 4 }}>masked</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Phone">
            <span data-testid="profile-phone">
              {profile.phone ?? '—'}
              {isMasked(profile.phone) && <Tag style={{ marginLeft: 4 }}>masked</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Permanent Address">
            <span data-testid="profile-permanent-address">
              {profile.permanentAddress ?? '—'}
              {isMasked(profile.permanentAddress) && <Tag style={{ marginLeft: 4 }}>masked</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Contact Address">
            <span data-testid="profile-contact-address">
              {profile.contactAddress ?? '—'}
              {isMasked(profile.contactAddress) && <Tag style={{ marginLeft: 4 }}>masked</Tag>}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Date of Birth">{profile.dob ?? profile.dobPartial ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="DOB Precision">{profile.dobPrecision ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="CCCD Issue Date">{profile.cccdIssueDate ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="CCCD Issue Place">{profile.cccdIssuePlace ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Tax Code">{profile.taxCode ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Hometown">{profile.hometown ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Death Date (Solar)">{profile.deathDateSolar ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Death Date (Lunar)">{profile.deathDateLunar ?? '—'}</Descriptions.Item>
          <Descriptions.Item label="Death Place">{profile.deathPlace ?? '—'}</Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title="Company Contexts" data-testid="company-contexts-card">
        <Space style={{ marginBottom: 8 }}>
          {hasPermission('CUSTOMER_CREATE_FINAL', 'GLOBAL') && (
            <Button type="primary" size="small" data-testid="add-context-btn" disabled>
              Add Company Context
            </Button>
          )}
        </Space>
        {contextsLoading && <Spin />}
        {contexts && contexts.length === 0 && (
          <Alert type="info" message="No company contexts." data-testid="no-contexts" />
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
