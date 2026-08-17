import React, { useState } from 'react';
import { Alert, Button, Card, Input, Popconfirm, Select, Space, Table, Typography, Upload, notification } from 'antd';
import { DeleteOutlined, UploadOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usePermissions } from '../auth/AuthProvider';
import { useCompany } from '../auth/CompanyProvider';
import { getCemeteries, setCemeteryWatermark, WATERMARK_OPTIONS, type CemeteryDto } from './cemeteriesApi';
import { listWatermarks, uploadWatermark, deleteWatermark, fetchWatermarkThumb, type CardWatermarkDto } from './watermarksApi';
import { getCards, fetchCardPdf } from '../cards/cardsApi';
import { getErrorMessage } from '../cards/errorMessages';

const { Title, Paragraph, Text } = Typography;

const WatermarkThumb: React.FC<{ companyId: number; id: number }> = ({ companyId, id }) => {
  const { data } = useQuery({
    queryKey: ['watermark-thumb', companyId, id],
    queryFn: async () => {
      const blob = await fetchWatermarkThumb(companyId, id);
      return URL.createObjectURL(blob);
    },
  });
  return (
    <div style={{ width: 96, height: 96, display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#fafafa', border: '1px solid #f0f0f0' }}>
      {data ? <img src={data} alt="hoa văn" style={{ maxWidth: '100%', maxHeight: '100%' }} /> : null}
    </div>
  );
};

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

  const { data: watermarks } = useQuery({
    queryKey: ['watermarks', currentCompanyId],
    queryFn: () => listWatermarks(currentCompanyId!),
    enabled: !!currentCompanyId,
  });

  const { data: cards } = useQuery({
    queryKey: ['cards', currentCompanyId],
    queryFn: () => getCards(currentCompanyId!),
    enabled: !!currentCompanyId,
  });
  const sampleCardId = cards && cards.length > 0 ? cards[0].id : null;

  // gộp mẫu dựng sẵn + mẫu tải lên
  const options = [
    ...WATERMARK_OPTIONS,
    ...(watermarks ?? []).map((w) => ({ value: w.code, label: `Tải lên: ${w.name}` })),
  ];

  const saveMutation = useMutation({
    mutationFn: ({ id, code }: { id: number; code: string }) => setCemeteryWatermark(currentCompanyId!, id, code || null),
    onSuccess: () => {
      notification.success({ message: 'Đã lưu hoa văn cho nghĩa trang.' });
      queryClient.invalidateQueries({ queryKey: ['cemeteries', currentCompanyId] });
    },
    onError: (err) => notification.error({ message: getErrorMessage(err) }),
  });

  // ── upload ──
  const [wmName, setWmName] = useState('');
  const [fileList, setFileList] = useState<UploadFile[]>([]);

  const uploadMutation = useMutation({
    mutationFn: ({ name, file }: { name: string; file: File }) => uploadWatermark(currentCompanyId!, name, file),
    onSuccess: () => {
      notification.success({ message: 'Đã tải mẫu hoa văn lên.' });
      setWmName(''); setFileList([]);
      queryClient.invalidateQueries({ queryKey: ['watermarks', currentCompanyId] });
    },
    onError: (err) => notification.error({ message: getErrorMessage(err) }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteWatermark(currentCompanyId!, id),
    onSuccess: () => {
      notification.success({ message: 'Đã xoá mẫu hoa văn.' });
      queryClient.invalidateQueries({ queryKey: ['watermarks', currentCompanyId] });
      queryClient.invalidateQueries({ queryKey: ['cemeteries', currentCompanyId] });
    },
    onError: (err) => notification.error({ message: getErrorMessage(err) }),
  });

  const onUpload = () => {
    const file = fileList[0]?.originFileObj as File | undefined;
    if (!wmName.trim() || !file) return;
    uploadMutation.mutate({ name: wmName.trim(), file });
  };

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
          style={{ width: 220 }}
          value={r.cardWatermarkCode ?? ''}
          disabled={!canManage}
          options={options}
          onChange={(code) => saveMutation.mutate({ id: r.id, code })}
          data-testid={`wm-select-${r.id}`}
        />
      ),
    },
    {
      title: 'Xem thử', key: 'preview',
      render: (_: unknown, r: CemeteryDto) => (
        <Button size="small" disabled={!sampleCardId} onClick={() => onPreview(r.cardWatermarkCode)} data-testid={`wm-preview-${r.id}`}>
          Xem thử
        </Button>
      ),
    },
  ];

  return (
    <div data-testid="cemetery-watermark-page">
      <Title level={3}>Hoa văn thẻ mộ</Title>
      <Paragraph type="secondary">
        Chọn hoa văn chìm cho thẻ của từng nghĩa trang (dùng mẫu dựng sẵn hoặc mẫu tải lên).
        Thay đổi lưu ngay; bấm <b>Xem thử</b> để mở bản in mẫu. Các nghĩa trang có thể dùng chung một mẫu.
      </Paragraph>

      {canManage && (
        <Card size="small" title="Thư viện mẫu tải lên (dùng chung trong công ty)" style={{ marginBottom: 16 }}>
          <Space align="start" wrap>
            <Input
              placeholder="Tên mẫu (vd: Trống đồng)" value={wmName}
              onChange={(e) => setWmName(e.target.value)} style={{ width: 220 }}
              data-testid="wm-upload-name"
            />
            <Upload
              accept="image/png,image/jpeg"
              maxCount={1}
              fileList={fileList}
              beforeUpload={() => false}
              onChange={({ fileList: fl }) => setFileList(fl.slice(-1))}
            >
              <Button icon={<UploadOutlined />}>Chọn ảnh (PNG/JPEG, ≤3MB)</Button>
            </Upload>
            <Button
              type="primary" onClick={onUpload}
              disabled={!wmName.trim() || fileList.length === 0}
              loading={uploadMutation.isPending}
              data-testid="wm-upload-btn"
            >
              Tải lên
            </Button>
          </Space>

          <div style={{ marginTop: 16, display: 'flex', flexWrap: 'wrap', gap: 16 }}>
            {(watermarks ?? []).map((w: CardWatermarkDto) => (
              <div key={w.id} style={{ textAlign: 'center' }}>
                {currentCompanyId && <WatermarkThumb companyId={currentCompanyId} id={w.id} />}
                <div style={{ maxWidth: 96, marginTop: 4 }}><Text ellipsis style={{ fontSize: 12 }}>{w.name}</Text></div>
                <Popconfirm title="Xoá mẫu này?" onConfirm={() => deleteMutation.mutate(w.id)} okText="Xoá" cancelText="Huỷ">
                  <Button size="small" danger type="text" icon={<DeleteOutlined />} data-testid={`wm-del-${w.id}`}>Xoá</Button>
                </Popconfirm>
              </div>
            ))}
            {(watermarks ?? []).length === 0 && <Text type="secondary">Chưa có mẫu tải lên.</Text>}
          </div>
        </Card>
      )}

      {!sampleCardId && (
        <Alert type="info" showIcon style={{ marginBottom: 16 }}
          message="Chưa có thẻ nào để xem thử. Cấp ít nhất một thẻ ở mục Thẻ mộ để bật xem thử." />
      )}
      {error && <Alert type="error" message={getErrorMessage(error)} style={{ marginBottom: 16 }} />}

      <Table<CemeteryDto> rowKey="id" loading={isLoading} dataSource={cemeteries ?? []} columns={columns} pagination={false} />
    </div>
  );
};

export default CemeteryWatermarkPage;
