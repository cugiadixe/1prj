import React, { useState } from 'react';
import {
  Alert,
  Button,
  DatePicker,
  Drawer,
  Input,
  Space,
  Spin,
  Table,
  Typography,
  Descriptions,
} from 'antd';
import { useQuery } from '@tanstack/react-query';
import { getAuditEvents } from './auditViewerApi';
import type { SecurityAuditEventDto } from './auditViewerApi';
import { getAuditViewerErrorMessage } from './errorMessages';
import dayjs, { Dayjs } from 'dayjs';

const { Title } = Typography;
const { RangePicker } = DatePicker;

const PAGE_SIZE = 50;

const AuditViewerPage: React.FC = () => {
  const [page, setPage] = useState(1);
  const [dates, setDates] = useState<[Dayjs | null, Dayjs | null] | null>(null);
  const [actorUserId, setActorUserId] = useState<number | undefined>();
  const [targetUserId, setTargetUserId] = useState<number | undefined>();
  const [eventType, setEventType] = useState<string | undefined>();
  const [entityType, setEntityType] = useState<string | undefined>();
  const [entityId, setEntityId] = useState<string | undefined>();
  const [correlationId, setCorrelationId] = useState<string | undefined>();

  const [selectedEvent, setSelectedEvent] = useState<SecurityAuditEventDto | null>(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: [
      'audit-events',
      page,
      dates?.[0]?.toISOString(),
      dates?.[1]?.toISOString(),
      actorUserId,
      targetUserId,
      eventType,
      entityType,
      entityId,
      correlationId,
    ],
    queryFn: () =>
      getAuditEvents({
        page,
        pageSize: PAGE_SIZE,
        fromUtc: dates?.[0] ? dates[0].toISOString() : undefined,
        toUtc: dates?.[1] ? dates[1].toISOString() : undefined,
        actorUserId,
        targetUserId,
        eventType,
        entityType,
        entityId,
        correlationId,
      }),
    retry: false,
  });

  const columns = [
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => dayjs(text).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: 'Sự kiện',
      dataIndex: 'eventCode',
      key: 'eventCode',
    },
    {
      title: 'Người thực hiện',
      dataIndex: 'actorUserId',
      key: 'actorUserId',
    },
    {
      title: 'Đối tượng',
      dataIndex: 'targetUserId',
      key: 'targetUserId',
    },
    {
      title: 'Thực thể',
      key: 'entity',
      render: (_: unknown, record: SecurityAuditEventDto) => (
        <span>
          {record.entityType} {record.entityId ? `(${record.entityId})` : ''}
        </span>
      ),
    },
    {
      title: 'Kết quả',
      dataIndex: 'outcome',
      key: 'outcome',
    },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, record: SecurityAuditEventDto) => (
        <Button
          type="link"
          size="small"
          onClick={() => setSelectedEvent(record)}
          data-testid={`view-audit-detail-${record.id}`}
        >
          Xem
        </Button>
      ),
    },
  ];

  if (isError) {
    return (
      <div data-testid="audit-list-error">
        <Alert type="error" message={getAuditViewerErrorMessage(error)} />
      </div>
    );
  }

  return (
    <div data-testid="audit-viewer-page">
      <Title level={3}>Nhật ký kiểm toán</Title>

      <Space style={{ marginBottom: 16 }} wrap>
        <RangePicker
          showTime
          onChange={(values) => {
            setDates(values as [Dayjs | null, Dayjs | null] | null);
            setPage(1);
          }}
          data-testid="audit-filter-dates"
        />
        <Input
          placeholder="Mã người thực hiện"
          type="number"
          allowClear
          onChange={(e) => {
            setActorUserId(e.target.value ? Number(e.target.value) : undefined);
            setPage(1);
          }}
          data-testid="audit-filter-actor"
        />
        <Input
          placeholder="Mã đối tượng"
          type="number"
          allowClear
          onChange={(e) => {
            setTargetUserId(e.target.value ? Number(e.target.value) : undefined);
            setPage(1);
          }}
          data-testid="audit-filter-target"
        />
        <Input
          placeholder="Loại sự kiện"
          allowClear
          onChange={(e) => {
            setEventType(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-eventtype"
        />
        <Input
          placeholder="Loại thực thể"
          allowClear
          onChange={(e) => {
            setEntityType(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-entitytype"
        />
        <Input
          placeholder="Mã thực thể"
          allowClear
          onChange={(e) => {
            setEntityId(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-entityid"
        />
        <Input
          placeholder="Mã tương quan"
          allowClear
          onChange={(e) => {
            setCorrelationId(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-correlation"
        />
      </Space>

      {isLoading && (
        <div style={{ textAlign: 'center', padding: 48 }} data-testid="audit-list-loading">
          <Spin size="large" />
        </div>
      )}

      {!isLoading && data && (
        <Table<SecurityAuditEventDto>
          dataSource={data.items}
          columns={columns}
          rowKey="id"
          data-testid="audit-list-table"
          locale={{ emptyText: 'Không có sự kiện kiểm toán.' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: data.totalCount,
            onChange: (p) => setPage(p),
            showTotal: (total) => `Tổng ${total} sự kiện`,
          }}
        />
      )}

      <Drawer
        title="Chi tiết sự kiện kiểm toán"
        placement="right"
        width={500}
        onClose={() => setSelectedEvent(null)}
        open={!!selectedEvent}
        data-testid="audit-detail-drawer"
      >
        {selectedEvent && (
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="ID">{selectedEvent.id}</Descriptions.Item>
            <Descriptions.Item label="Thời gian">{dayjs(selectedEvent.createdAt).format('YYYY-MM-DD HH:mm:ss')}</Descriptions.Item>
            <Descriptions.Item label="Mã sự kiện">{selectedEvent.eventCode}</Descriptions.Item>
            <Descriptions.Item label="Người thực hiện">{selectedEvent.actorUserId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Thay mặt">{selectedEvent.actingAsUserId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Đối tượng">{selectedEvent.targetUserId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Công ty">{selectedEvent.companyId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Loại thực thể">{selectedEvent.entityType}</Descriptions.Item>
            <Descriptions.Item label="Mã thực thể">{selectedEvent.entityId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Kết quả">{selectedEvent.outcome}</Descriptions.Item>
            <Descriptions.Item label="Lý do">{selectedEvent.reason ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Mã tương quan">{selectedEvent.correlationId}</Descriptions.Item>
            <Descriptions.Item label="Phiên bản chính sách">{selectedEvent.policyVersion ?? 'N/A'}</Descriptions.Item>
          </Descriptions>
        )}
      </Drawer>
    </div>
  );
};

export default AuditViewerPage;
