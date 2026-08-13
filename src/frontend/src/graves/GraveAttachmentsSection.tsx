import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, Image, Popconfirm, Space, Spin, Typography, Upload, message } from 'antd';
import { DeleteOutlined, FilePdfOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UploadProps } from 'antd';
import { usePermissions } from '../auth/AuthProvider';
import { deleteAttachment, fetchAttachmentObjectUrl, listAttachments, uploadAttachment } from './attachmentsApi';
import { getErrorMessage } from './errorMessages';
import type { GraveAttachment } from './types';

const { Text } = Typography;
const MAX_MB = 10;
const ACCEPT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'];

const GraveAttachmentsSection: React.FC<{ graveId: number }> = ({ graveId }) => {
  const { hasPermission } = usePermissions();
  const canManage = hasPermission('GRAVE_ATTACHMENT_MANAGE', 'GLOBAL');
  const queryClient = useQueryClient();

  const { data: attachments, isLoading } = useQuery({
    queryKey: ['grave-attachments', graveId],
    queryFn: () => listAttachments(graveId),
    enabled: !Number.isNaN(graveId),
  });

  // Tải blob (kèm token) cho ảnh → object URL để hiển thị thumbnail + xem to
  const [imgUrls, setImgUrls] = useState<Record<number, { thumb: string; full: string }>>({});
  useEffect(() => {
    if (!attachments) return;
    let cancelled = false;
    const created: string[] = [];
    (async () => {
      const map: Record<number, { thumb: string; full: string }> = {};
      for (const a of attachments.filter((x) => x.isImage)) {
        try {
          const thumb = await fetchAttachmentObjectUrl(graveId, a.id, true);
          const full = await fetchAttachmentObjectUrl(graveId, a.id, false);
          created.push(thumb, full);
          map[a.id] = { thumb, full };
        } catch { /* bỏ qua ảnh lỗi */ }
      }
      if (cancelled) created.forEach(URL.revokeObjectURL);
      else setImgUrls(map);
    })();
    return () => { cancelled = true; created.forEach(URL.revokeObjectURL); };
  }, [attachments, graveId]);

  const deleteMut = useMutation({
    mutationFn: (id: number) => deleteAttachment(graveId, id),
    onSuccess: () => {
      message.success('Đã xóa file');
      queryClient.invalidateQueries({ queryKey: ['grave-attachments', graveId] });
    },
    onError: (e) => message.error(getErrorMessage(e)),
  });

  const openDoc = async (a: GraveAttachment) => {
    try {
      const url = await fetchAttachmentObjectUrl(graveId, a.id, false);
      window.open(url, '_blank', 'noopener');
    } catch (e) { message.error(getErrorMessage(e)); }
  };

  const uploadProps: UploadProps = {
    showUploadList: false,
    accept: '.jpg,.jpeg,.png,.webp,.pdf',
    beforeUpload: (file) => {
      if (!ACCEPT_TYPES.includes(file.type)) {
        message.error('Chỉ nhận ảnh JPG/PNG/WEBP hoặc PDF.');
        return Upload.LIST_IGNORE;
      }
      if (file.size > MAX_MB * 1024 * 1024) {
        message.error(`File vượt quá ${MAX_MB}MB.`);
        return Upload.LIST_IGNORE;
      }
      return true;
    },
    customRequest: async ({ file, onSuccess, onError }) => {
      try {
        const f = file as File;
        const category = f.type.startsWith('image/') ? 'PHOTO' : 'OTHER';
        await uploadAttachment(graveId, f, category);
        message.success('Đã tải lên');
        queryClient.invalidateQueries({ queryKey: ['grave-attachments', graveId] });
        onSuccess?.({});
      } catch (e) {
        message.error(getErrorMessage(e));
        onError?.(e as Error);
      }
    },
  };

  const images = (attachments ?? []).filter((a) => a.isImage);
  const docs = (attachments ?? []).filter((a) => !a.isImage);

  return (
    <Card
      title="Ảnh & tài liệu"
      style={{ marginTop: 16 }}
      data-testid="grave-attachments-card"
      extra={canManage && (
        <Upload {...uploadProps}>
          <Button type="primary" icon={<UploadOutlined />} data-testid="upload-attachment-btn">Tải lên</Button>
        </Upload>
      )}
    >
      {isLoading && <Spin />}
      {!isLoading && (attachments?.length ?? 0) === 0 && (
        <Alert type="info" message="Chưa có ảnh hoặc tài liệu nào cho phần mộ này." />
      )}

      {images.length > 0 && (
        <Image.PreviewGroup>
          <Space wrap size={12}>
            {images.map((a) => (
              <div key={a.id} style={{ position: 'relative', width: 96 }}>
                {imgUrls[a.id] ? (
                  <Image
                    width={96}
                    height={96}
                    style={{ objectFit: 'cover', borderRadius: 6 }}
                    src={imgUrls[a.id].thumb}
                    preview={{ src: imgUrls[a.id].full }}
                    alt={a.fileNameOriginal}
                  />
                ) : (
                  <div style={{ width: 96, height: 96, display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#f5f5f5', borderRadius: 6 }}>
                    <Spin size="small" />
                  </div>
                )}
                {canManage && (
                  <Popconfirm title="Xóa ảnh này?" onConfirm={() => deleteMut.mutate(a.id)} okText="Xóa" cancelText="Hủy">
                    <Button size="small" danger icon={<DeleteOutlined />} style={{ position: 'absolute', top: 2, right: 2 }} />
                  </Popconfirm>
                )}
              </div>
            ))}
          </Space>
        </Image.PreviewGroup>
      )}

      {docs.length > 0 && (
        <Space direction="vertical" style={{ width: '100%', marginTop: images.length ? 16 : 0 }}>
          {docs.map((a) => (
            <Space key={a.id} style={{ justifyContent: 'space-between', width: '100%' }}>
              <Button type="link" icon={<FilePdfOutlined />} onClick={() => openDoc(a)} style={{ padding: 0 }}>
                {a.fileNameOriginal}
                {a.category === 'TRANSFER_DOC' && <Text type="secondary"> (văn bản chuyển quyền)</Text>}
              </Button>
              <Space>
                <Text type="secondary">{(a.sizeBytes / 1024).toFixed(0)} KB</Text>
                {canManage && (
                  <Popconfirm title="Xóa tài liệu này?" onConfirm={() => deleteMut.mutate(a.id)} okText="Xóa" cancelText="Hủy">
                    <Button size="small" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                )}
              </Space>
            </Space>
          ))}
        </Space>
      )}
    </Card>
  );
};

export default GraveAttachmentsSection;
