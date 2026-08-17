import React, { useState } from 'react';
import { Alert, Button, Modal, Select, Space, Table, Tag, Typography, notification } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { usePermissions } from '../auth/AuthProvider';
import { searchGraves } from '../graves/gravesApi';
import { useCards, useCreateCard } from './cardsHooks';
import { useCreateCardReprintRequest, usePrintInitialCardReprint } from './hooks';
import { getErrorMessage } from './errorMessages';
import type { CardDto } from './cardsApi';

const { Title, Paragraph } = Typography;

const STATUS_COLOR: Record<string, string> = {
  ACTIVE: 'green',
  INACTIVE: 'default',
  REVOKED: 'red',
};

const CardsPage: React.FC = () => {
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const canIssue = hasPermission('CARD_ISSUE');

  const { data: cards, isLoading, error } = useCards();
  const createCard = useCreateCard();
  const createReprint = useCreateCardReprintRequest();
  const printInitial = usePrintInitialCardReprint();

  const [isIssueOpen, setIsIssueOpen] = useState(false);
  const [graveSearch, setGraveSearch] = useState('');
  const [selectedGraveId, setSelectedGraveId] = useState<number | undefined>(undefined);
  const [busyCardId, setBusyCardId] = useState<number | null>(null);

  const { data: graveResults, isLoading: gravesLoading } = useQuery({
    queryKey: ['graves-picker', graveSearch],
    queryFn: () => searchGraves({ search: graveSearch || undefined, pageSize: 20 }),
    enabled: isIssueOpen,
  });

  const onIssue = async () => {
    if (!selectedGraveId) return;
    try {
      const card = await createCard.mutateAsync({ graveId: selectedGraveId });
      notification.success({ message: `Đã cấp thẻ số ${card.cardNumber ?? card.id}` });
      setIsIssueOpen(false);
      setSelectedGraveId(undefined);
      setGraveSearch('');
    } catch (err) {
      notification.error({ message: getErrorMessage(err) });
    }
  };

  // In lần đầu (miễn duyệt): tạo yêu cầu INITIAL rồi in thẳng.
  const onPrintInitial = async (card: CardDto) => {
    setBusyCardId(card.id);
    try {
      const req = await createReprint.mutateAsync({ cardId: card.id });
      await printInitial.mutateAsync(req.id);
      notification.success({ message: 'Đã in lần đầu (miễn duyệt).' });
    } catch (err) {
      notification.error({ message: getErrorMessage(err) });
    } finally {
      setBusyCardId(null);
    }
  };

  // In lại (cần duyệt + phí): tạo yêu cầu REPRINT rồi mở trang chi tiết để Gửi duyệt.
  const onRequestReprint = async (card: CardDto) => {
    setBusyCardId(card.id);
    try {
      const req = await createReprint.mutateAsync({ cardId: card.id });
      notification.info({ message: 'Đã tạo yêu cầu in lại — bấm "Gửi" để trình duyệt.' });
      navigate(`/cards/reprints/${req.id}`);
    } catch (err) {
      notification.error({ message: getErrorMessage(err) });
    } finally {
      setBusyCardId(null);
    }
  };

  const columns = [
    { title: 'Số thẻ', dataIndex: 'cardNumber', key: 'cardNumber', render: (v: string | null) => v || '—' },
    { title: 'Mã mộ', dataIndex: 'graveId', key: 'graveId', render: (v: string | null) => v || '—' },
    {
      title: 'Số lần in', dataIndex: 'printCount', key: 'printCount', align: 'right' as const,
      render: (n: number) => (n === 0 ? <Tag>Chưa in</Tag> : <b>{n}</b>),
    },
    {
      title: 'Trạng thái', dataIndex: 'status', key: 'status',
      render: (s: string) => <Tag color={STATUS_COLOR[s] ?? 'default'}>{s}</Tag>,
    },
    {
      title: 'Thao tác', key: 'actions',
      render: (_: unknown, card: CardDto) => (
        <Space>
          {card.printCount === 0 ? (
            <Button
              type="primary"
              size="small"
              loading={busyCardId === card.id}
              onClick={() => onPrintInitial(card)}
              data-testid={`btn-print-initial-${card.id}`}
            >
              In lần đầu (miễn duyệt)
            </Button>
          ) : (
            <Button
              size="small"
              loading={busyCardId === card.id}
              onClick={() => onRequestReprint(card)}
              data-testid={`btn-request-reprint-${card.id}`}
            >
              Yêu cầu in lại (cần duyệt)
            </Button>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div data-testid="cards-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={3} style={{ margin: 0 }}>Thẻ mộ</Title>
        <Space>
          <Button onClick={() => navigate('/cards/reprints')}>Danh sách yêu cầu in lại</Button>
          {canIssue && (
            <Button type="primary" onClick={() => setIsIssueOpen(true)} data-testid="btn-issue-card">
              Cấp thẻ mới
            </Button>
          )}
        </Space>
      </Space>

      <Paragraph type="secondary">
        Mỗi phần mộ có một thẻ. <b>In lần đầu</b> miễn duyệt, miễn phí. <b>In lại</b> (từ lần 2) phải qua duyệt và thu phí.
      </Paragraph>

      {error && <Alert type="error" message={getErrorMessage(error)} style={{ marginBottom: 16 }} data-testid="cards-error" />}

      <Table<CardDto>
        rowKey="id"
        loading={isLoading}
        dataSource={cards ?? []}
        columns={columns}
        pagination={{ pageSize: 20 }}
      />

      <Modal
        title="Cấp thẻ mới từ phần mộ"
        open={isIssueOpen}
        onOk={onIssue}
        okButtonProps={{ disabled: !selectedGraveId, loading: createCard.isPending }}
        onCancel={() => setIsIssueOpen(false)}
        okText="Cấp thẻ"
        cancelText="Hủy"
      >
        <Select
          showSearch
          style={{ width: '100%' }}
          placeholder="Tìm phần mộ theo mã mộ / chủ mộ"
          filterOption={false}
          onSearch={setGraveSearch}
          loading={gravesLoading}
          value={selectedGraveId}
          onChange={setSelectedGraveId}
          data-testid="grave-picker"
          options={(graveResults?.items ?? []).map((g) => ({
            value: g.id,
            label: `${g.graveCode} · Khu ${g.zone}${g.ownerName ? ` · ${g.ownerName}` : ''}`,
          }))}
        />
      </Modal>
    </div>
  );
};

export default CardsPage;
