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
      title: 'Time',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => dayjs(text).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: 'Event',
      dataIndex: 'eventCode',
      key: 'eventCode',
    },
    {
      title: 'Actor ID',
      dataIndex: 'actorUserId',
      key: 'actorUserId',
    },
    {
      title: 'Target ID',
      dataIndex: 'targetUserId',
      key: 'targetUserId',
    },
    {
      title: 'Entity',
      key: 'entity',
      render: (_: unknown, record: SecurityAuditEventDto) => (
        <span>
          {record.entityType} {record.entityId ? `(${record.entityId})` : ''}
        </span>
      ),
    },
    {
      title: 'Outcome',
      dataIndex: 'outcome',
      key: 'outcome',
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: SecurityAuditEventDto) => (
        <Button
          type="link"
          size="small"
          onClick={() => setSelectedEvent(record)}
          data-testid={`view-audit-detail-${record.id}`}
        >
          View
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
      <Title level={3}>Security Audit Viewer</Title>

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
          placeholder="Actor ID"
          type="number"
          allowClear
          onChange={(e) => {
            setActorUserId(e.target.value ? Number(e.target.value) : undefined);
            setPage(1);
          }}
          data-testid="audit-filter-actor"
        />
        <Input
          placeholder="Target ID"
          type="number"
          allowClear
          onChange={(e) => {
            setTargetUserId(e.target.value ? Number(e.target.value) : undefined);
            setPage(1);
          }}
          data-testid="audit-filter-target"
        />
        <Input
          placeholder="Event Type"
          allowClear
          onChange={(e) => {
            setEventType(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-eventtype"
        />
        <Input
          placeholder="Entity Type"
          allowClear
          onChange={(e) => {
            setEntityType(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-entitytype"
        />
        <Input
          placeholder="Entity ID"
          allowClear
          onChange={(e) => {
            setEntityId(e.target.value || undefined);
            setPage(1);
          }}
          data-testid="audit-filter-entityid"
        />
        <Input
          placeholder="Correlation ID"
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
          locale={{ emptyText: 'No audit events found.' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: data.totalCount,
            onChange: (p) => setPage(p),
            showTotal: (total) => `Total ${total} events`,
          }}
        />
      )}

      <Drawer
        title="Audit Event Detail"
        placement="right"
        width={500}
        onClose={() => setSelectedEvent(null)}
        open={!!selectedEvent}
        data-testid="audit-detail-drawer"
      >
        {selectedEvent && (
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="ID">{selectedEvent.id}</Descriptions.Item>
            <Descriptions.Item label="Time">{dayjs(selectedEvent.createdAt).format('YYYY-MM-DD HH:mm:ss')}</Descriptions.Item>
            <Descriptions.Item label="Event Code">{selectedEvent.eventCode}</Descriptions.Item>
            <Descriptions.Item label="Actor User ID">{selectedEvent.actorUserId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Acting As User ID">{selectedEvent.actingAsUserId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Target User ID">{selectedEvent.targetUserId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Company ID">{selectedEvent.companyId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Entity Type">{selectedEvent.entityType}</Descriptions.Item>
            <Descriptions.Item label="Entity ID">{selectedEvent.entityId ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Outcome">{selectedEvent.outcome}</Descriptions.Item>
            <Descriptions.Item label="Reason">{selectedEvent.reason ?? 'N/A'}</Descriptions.Item>
            <Descriptions.Item label="Correlation ID">{selectedEvent.correlationId}</Descriptions.Item>
            <Descriptions.Item label="Policy Version">{selectedEvent.policyVersion ?? 'N/A'}</Descriptions.Item>
          </Descriptions>
        )}
      </Drawer>
    </div>
  );
};

export default AuditViewerPage;
