import React, { useState } from 'react';
import { Input, Space, Table, Tag, Typography } from 'antd';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getAttachmentSummary, type GraveAttachmentSummary } from './gravesApi';
import { listAttachments } from './attachmentsApi';
import type { GraveAttachment } from './types';
import { formatUtcDateTime } from '../utils/datetime';

const { Title, Paragraph } = Typography;
const PAGE_SIZE = 20;

const CATEGORY_LABEL: Record<string, string> = {
  PHOTO: 'Ảnh',
  TRANSFER_DOC: 'VB chuyển nhượng',
  OTHER: 'Khác',
};

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
        { title: 'Ngày tải lên', dataIndex: 'createdAt', key: 'createdAt', render: (v: string) => formatUtcDateTime(v) },
      ]}
    />
  );
};

const GraveAttachmentSummaryPage: React.FC = () => {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['grave-attachment-summary', search, page],
    queryFn: () => getAttachmentSummary({ search: search || undefined, page, pageSize: PAGE_SIZE }),
  });

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

      <Space style={{ marginBottom: 16 }}>
        <Input.Search
          allowClear style={{ width: 320 }} placeholder="Tìm theo mã mộ"
          onSearch={(v) => { setSearch(v); setPage(1); }}
          onChange={(e) => { if (!e.target.value) { setSearch(''); setPage(1); } }}
          data-testid="attachment-summary-search"
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
