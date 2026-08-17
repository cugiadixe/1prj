import React from 'react';
import { Alert, Button, Select, Table, Typography, notification } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usePermissions } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { getCemeteries, setCemeteryWatermark, WATERMARK_OPTIONS, type CemeteryDto } from './cemeteriesApi';
import { getCards, fetchCardPdf } from '../cards/cardsApi';
import { getErrorMessage } from '../cards/errorMessages';

const { Title, Paragraph } = Typography;

const CemeteryWatermarkPage: React.FC = () => {
  const { hasPermission } = usePermissions();
  const { currentCompanyId } = useCompany();
  const queryClient = useQueryClient();
  const canManage = hasPermission('CARD_ISSUE');

  const { data: cemeteries, isLoading, error } = useQuery({
    queryKey: ['cemeteries', currentCompanyId],
    queryFn: () => getCemeteries(currentCompanyId!),
    enabled: !!currentCompanyId,
  });

  // Lấy 1 thẻ bất kỳ của công ty để xem thử hoa văn.
  const { data: cards } = useQuery({
    queryKey: ['cards', currentCompanyId],
    queryFn: () => getCards(currentCompanyId!),
    enabled: !!currentCompanyId,
  });
  const sampleCardId = cards && cards.length > 0 ? cards[0].id : null;

  const saveMutation = useMutation({
    mutationFn: ({ id, code }: { id: number; code: string }) =>
      setCemeteryWatermark(currentCompanyId!, id, code || null),
    onSuccess: () => {
      notification.success({ message: 'Đã lưu hoa văn cho nghĩa trang.' });
      queryClient.invalidateQueries({ queryKey: ['cemeteries', currentCompanyId] });
    },
    onError: (err) => notification.error({ message: getErrorMessage(err) }),
  });

  const onPreview = async (code: string | null) => {
    if (!currentCompanyId || !sampleCardId) return;
    try {
      const blob = await fetchCardPdf(currentCompanyId, sampleCardId, code || undefined);
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank', 'noopener');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (err) {
      notification.error({ message: getErrorMessage(err) });
    }
  };

  const columns = [
    { title: 'Mã', dataIndex: 'cemeteryCode', key: 'cemeteryCode' },
    { title: 'Nghĩa trang', dataIndex: 'name', key: 'name' },
    {
      title: 'Hoa văn chìm', key: 'watermark',
      render: (_: unknown, r: CemeteryDto) => (
        <Select
          style={{ width: 200 }}
          value={r.cardWatermarkCode ?? ''}
          disabled={!canManage}
          options={WATERMARK_OPTIONS}
          onChange={(code) => saveMutation.mutate({ id: r.id, code })}
          data-testid={`wm-select-${r.id}`}
        />
      ),
    },
    {
      title: 'Xem thử', key: 'preview',
      render: (_: unknown, r: CemeteryDto) => (
        <Button
          size="small"
          disabled={!sampleCardId}
          onClick={() => onPreview(r.cardWatermarkCode)}
          data-testid={`wm-preview-${r.id}`}
        >
          Xem thử
        </Button>
      ),
    },
  ];

  return (
    <div data-testid="cemetery-watermark-page">
      <Title level={3}>Hoa văn thẻ mộ (theo nghĩa trang)</Title>
      <Paragraph type="secondary">
        Chọn hoa văn chìm cho thẻ của từng nghĩa trang. Thay đổi được lưu ngay. Bấm <b>Xem thử</b> để mở
        bản in mẫu (dùng một thẻ có sẵn của công ty). Các nghĩa trang có thể dùng chung một mẫu.
      </Paragraph>

      {!sampleCardId && (
        <Alert
          type="info" showIcon style={{ marginBottom: 16 }}
          message="Chưa có thẻ nào để xem thử. Cấp ít nhất một thẻ ở mục Thẻ mộ để bật xem thử."
        />
      )}
      {error && <Alert type="error" message={getErrorMessage(error)} style={{ marginBottom: 16 }} />}

      <Table<CemeteryDto>
        rowKey="id"
        loading={isLoading}
        dataSource={cemeteries ?? []}
        columns={columns}
        pagination={false}
      />
    </div>
  );
};

export default CemeteryWatermarkPage;
