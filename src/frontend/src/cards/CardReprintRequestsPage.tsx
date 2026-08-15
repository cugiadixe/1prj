import React, { useState } from 'react';
import { Alert, Button, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { Link, useNavigate } from 'react-router-dom';
import { usePermissions } from '../auth/AuthProvider';
import { useCardReprintRequests } from './hooks';
import { getErrorMessage, isPermissionDenied } from './errorMessages';
import type { CardReprintRequestDto } from './types';

const { Title } = Typography;

const CardReprintRequestsPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState<string | undefined>(undefined);

  const { data, isLoading, error } = useCardReprintRequests(statusFilter ? { status: statusFilter } : undefined);

  if (isPermissionDenied(error)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền xem yêu cầu in lại thẻ."
        data-testid="permission-denied"
      />
    );
  }

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
    },
    {
      title: 'Mã thẻ',
      dataIndex: 'cardId',
      key: 'cardId',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        let color = 'default';
        if (status === 'DRAFT') color = 'default';
        else if (status === 'PENDING_APPROVAL') color = 'orange';
        else if (status === 'APPROVED') color = 'blue';
        else if (status === 'REJECTED') color = 'red';
        else if (status === 'PENDING_PAYMENT') color = 'purple';
        else if (status === 'PAID') color = 'cyan';
        else if (status === 'PRINTED') color = 'geekblue';
        else if (status === 'RELEASED') color = 'green';
        return <Tag color={color}>{status}</Tag>;
      },
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleString('vi-VN'),
    }
  ];

  return (
    <div data-testid="card-reprint-requests-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Yêu cầu in lại thẻ</Title>
        <Space>
          {hasPermission('CARD_REPRINT_REQUEST_CREATE') && (
            <Button type="primary" data-testid="create-card-reprint-btn">
              <Link to="/cards/reprints/new">Tạo yêu cầu</Link>
            </Button>
          )}
        </Space>
      </Space>

      <Space style={{ marginBottom: 16 }}>
        <Select
          placeholder="Lọc theo trạng thái"
          allowClear
          style={{ width: 200 }}
          onChange={(val) => setStatusFilter(val)}
          value={statusFilter}
          data-testid="card-reprint-status-filter"
          options={[
            { label: 'DRAFT', value: 'DRAFT' },
            { label: 'PENDING_APPROVAL', value: 'PENDING_APPROVAL' },
            { label: 'APPROVED', value: 'APPROVED' },
            { label: 'REJECTED', value: 'REJECTED' },
            { label: 'PENDING_PAYMENT', value: 'PENDING_PAYMENT' },
            { label: 'PAID', value: 'PAID' },
            { label: 'PRINTED', value: 'PRINTED' },
            { label: 'RELEASED', value: 'RELEASED' },
          ]}
        />
      </Space>

      {error && !isPermissionDenied(error) && (
        <Alert
          type="error"
          message={getErrorMessage(error)}
          style={{ marginBottom: 16 }}
          data-testid="card-reprint-list-error"
        />
      )}

      {isLoading && <Spin data-testid="card-reprint-list-loading" />}

      {!isLoading && !error && data && data.items.length === 0 && (
        <Alert
          type="info"
          message="Không tìm thấy yêu cầu nào."
          data-testid="card-reprint-list-empty"
        />
      )}

      {data && data.items.length > 0 && (
        <Table
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="card-reprint-list-table"
          onRow={(record: CardReprintRequestDto) => ({
            onClick: () => navigate(`/cards/reprints/${record.id}`),
            style: { cursor: 'pointer' },
          })}
          pagination={{
            total: data.totalCount,
          }}
        />
      )}
    </div>
  );
};

export default CardReprintRequestsPage;
