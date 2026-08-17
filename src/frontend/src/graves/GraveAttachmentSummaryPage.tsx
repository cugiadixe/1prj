import React, { useState } from 'react';
import { DatePicker, Input, Select, Space, Table, Tag, Typography } from 'antd';
import type { Dayjs } from 'dayjs';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getAttachmentSummary, getAttachmentUploaders, type GraveAttachmentSummary } from './gravesApi';
import { listAttachments } from './attachmentsApi';
import type { GraveAttachment } from './types';
import { formatUtcDateTime } from '../utils/datetime';

const { Title, Paragraph } = Typography;
const { RangePicker } = DatePicker;
const PAGE_SIZE = 20;

const CATEGORY_LABEL: Record<string, string> = {
  PHOTO: 'Ảnh',
  TRANSFER_DOC: 'VB chuyển nhượng',
  OTHER: 'Khác',
};

const ZONE_OPTIONS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L']
  .map((z) => ({ value: z, label: `Khu ${z}` }));
const CATEGORY_OPTIONS = [
  { value: 'PHOTO', label: 'Ảnh' },
  { value: 'TRANSFER_DOC', label: 'VB chuyển nhượng' },
  { value: 'OTHER', label: 'Khác' },
];

function formatSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${bytes} B`;
}

const ExpandedFiles: React.FC<{ graveId: number }> = ({ graveId }) => {
  const { data, isLoading } = useQuery({
    queryKey: ['grave-attachments', graveId],
    queryFn: () => listAttachments(graveId),
  });
  return (
    <Table<GraveAttachment>
      size="small"
      rowKey="id"
      loading={isLoading}
      dataSource={data ?? []}
      pagination={false}
      columns={[
        { title: 'Tên tài liệu', dataIndex: 'fileNameOriginal', key: 'name' },
        {
          title: 'Loại', dataIndex: 'category', key: 'category',
          render: (c: string) => <Tag>{CATEGORY_LABEL[c] ?? c}</Tag>,
        },
        { title: 'Kích thước', dataIndex: 'sizeBytes', key: 'size', render: (b: number) => formatSize(b) },
        { title: 'Người tải lên', dataIndex: 'uploadedByName', key: 'uploadedByName', render: (v: string | null) => v || '—' },
        { title: 'Ngày tải lên', dataIndex: 'createdAt', key: 'createdAt', render: (v: string) => formatUtcDateTime(v) },
      ]}
    />
  );
};

const GraveAttachmentSummaryPage: React.FC = () => {
  const [search, setSearch] = useState('');
  const [zone, setZone] = useState<string | undefined>(undefined);
  const [category, setCategory] = useState<string | undefined>(undefined);
  const [uploaderId, setUploaderId] = useState<number | undefined>(undefined);
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [page, setPage] = useState(1);

  const { data: uploaders } = useQuery({ queryKey: ['grave-attachment-uploaders'], queryFn: getAttachmentUploaders });

  const uploadedFrom = dateRange ? dateRange[0].startOf('day').toISOString() : undefined;
  const uploadedTo = dateRange ? dateRange[1].endOf('day').toISOString() : undefined;

  const { data, isLoading } = useQuery({
    queryKey: ['grave-attachment-summary', search, zone, category, uploaderId, uploadedFrom, uploadedTo, page],
    queryFn: () => getAttachmentSummary({
      search: search || undefined, zone, category, uploadedByUserId: uploaderId,
      uploadedFrom, uploadedTo, page, pageSize: PAGE_SIZE,
    }),
  });

  const resetPage = () => setPage(1);

  const columns = [
    {
      title: 'Mã mộ', dataIndex: 'graveCode', key: 'graveCode',
      render: (code: string, r: GraveAttachmentSummary) => <Link to={`/graves/${r.graveId}`}>{code}</Link>,
    },
    { title: 'Khu', dataIndex: 'zone', key: 'zone' },
    { title: 'Chủ mộ', dataIndex: 'ownerName', key: 'ownerName', render: (v: string | null) => v || '—' },
    { title: 'Nghĩa trang', dataIndex: 'cemeteryName', key: 'cemeteryName', render: (v: string | null) => v || '—' },
    { title: 'Ảnh', dataIndex: 'photoCount', key: 'photoCount', align: 'right' as const },
    { title: 'VB chuyển nhượng', dataIndex: 'transferDocCount', key: 'transferDocCount', align: 'right' as const },
    { title: 'Khác', dataIndex: 'otherCount', key: 'otherCount', align: 'right' as const },
    {
      title: 'Tổng', dataIndex: 'totalCount', key: 'totalCount', align: 'right' as const,
      render: (n: number) => <b>{n}</b>,
    },
    {
      title: 'Cập nhật gần nhất', dataIndex: 'lastUploadedAt', key: 'lastUploadedAt',
      render: (v: string | null) => (v ? formatUtcDateTime(v) : '—'),
    },
  ];

  return (
    <div>
      <Title level={3}>Tổng hợp giấy tờ / tài liệu theo mộ</Title>
      <Paragraph type="secondary">
        Danh sách các phần mộ ĐÃ có tài liệu đính kèm, kèm số lượng theo loại. Bấm dấu ▸ để xem chi tiết từng file;
        bấm mã mộ để mở trang mộ.
      </Paragraph>

      <Space style={{ marginBottom: 16 }} wrap>
        <Input.Search
          allowClear style={{ width: 300 }} placeholder="Tìm theo mã mộ / chủ mộ"
          onSearch={(v) => { setSearch(v); resetPage(); }}
          onChange={(e) => { if (!e.target.value) { setSearch(''); resetPage(); } }}
          data-testid="attachment-summary-search"
        />
        <Select
          allowClear style={{ width: 130 }} placeholder="Khu" value={zone}
          onChange={(v) => { setZone(v); resetPage(); }} options={ZONE_OPTIONS}
          data-testid="attachment-summary-zone"
        />
        <Select
          allowClear style={{ width: 190 }} placeholder="Loại tài liệu" value={category}
          onChange={(v) => { setCategory(v); resetPage(); }} options={CATEGORY_OPTIONS}
          data-testid="attachment-summary-category"
        />
        <Select
          allowClear showSearch optionFilterProp="label" style={{ width: 240 }} placeholder="Người tải lên"
          value={uploaderId} onChange={(v) => { setUploaderId(v); resetPage(); }}
          options={uploaders?.map((u) => ({ value: u.userId, label: u.name }))}
          data-testid="attachment-summary-uploader"
        />
        <RangePicker
          format="DD/MM/YYYY" placeholder={['Tải từ ngày', 'đến ngày']}
          value={dateRange}
          onChange={(v) => { setDateRange(v && v[0] && v[1] ? [v[0], v[1]] : null); resetPage(); }}
          data-testid="attachment-summary-daterange"
        />
      </Space>

      <Table<GraveAttachmentSummary>
        rowKey="graveId"
        loading={isLoading}
        dataSource={data?.items ?? []}
        columns={columns}
        expandable={{ expandedRowRender: (r) => <ExpandedFiles graveId={r.graveId} /> }}
        pagination={{
          current: page,
          pageSize: PAGE_SIZE,
          total: data?.totalCount ?? 0,
          onChange: setPage,
          showTotal: (t) => `${t} mộ có tài liệu`,
        }}
      />
    </div>
  );
};

export default GraveAttachmentSummaryPage;
