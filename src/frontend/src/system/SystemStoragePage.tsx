import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, Descriptions, Form, Input, Space, Typography, message } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import axiosClient from '../api/axiosClient';

const { Title, Paragraph, Text } = Typography;

interface StoragePathDto {
  configuredPath: string | null;
  defaultPath: string;
  effectivePath: string;
}

async function getStoragePath(): Promise<StoragePathDto> {
  const { data } = await axiosClient.get<StoragePathDto>('/system/settings/storage-path');
  return data;
}
async function setStoragePath(path: string | null): Promise<StoragePathDto> {
  const { data } = await axiosClient.put<StoragePathDto>('/system/settings/storage-path', { path });
  return data;
}

const SystemStoragePage: React.FC = () => {
  const qc = useQueryClient();
  const [path, setPath] = useState('');

  const { data, isLoading } = useQuery({ queryKey: ['system-storage-path'], queryFn: getStoragePath });

  useEffect(() => {
    if (data) setPath(data.configuredPath ?? '');
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: (p: string | null) => setStoragePath(p),
    onSuccess: (d) => {
      message.success('Đã lưu cấu hình đường dẫn lưu trữ');
      qc.setQueryData(['system-storage-path'], d);
    },
    onError: (err: unknown) => {
      const e = err as { response?: { data?: { detail?: string; title?: string } } };
      message.error(e?.response?.data?.detail || e?.response?.data?.title || 'Lưu cấu hình thất bại');
    },
  });

  return (
    <div style={{ maxWidth: 760 }}>
      <Title level={3}>Cấu hình lưu trữ file</Title>
      <Paragraph type="secondary">
        Đường dẫn thư mục gốc lưu ảnh/tài liệu đính kèm phần mộ. File thật lưu ở
        <Text code>{'{đường dẫn}\\graves\\{mã mộ}\\...'}</Text>. Chỉ quản trị viên chỉnh được.
      </Paragraph>

      <Alert
        type="warning"
        showIcon
        style={{ marginBottom: 16 }}
        message="Lưu ý khi đổi đường dẫn"
        description={
          <>
            Đổi đường dẫn <b>KHÔNG tự di chuyển</b> file đang có. File cũ vẫn nằm ở đường dẫn cũ;
            nếu không chép sang đường dẫn mới thì các đính kèm cũ sẽ không mở được. Nên: dừng cấp/xem
            đính kèm, chép toàn bộ thư mục <Text code>graves\</Text> từ chỗ cũ sang chỗ mới, rồi mới đổi.
          </>
        }
      />

      <Card loading={isLoading}>
        <Descriptions column={1} size="small" style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Đang dùng thực tế">
            <Text code>{data?.effectivePath}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="Mặc định (appsettings)">
            <Text code>{data?.defaultPath}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="Trạng thái">
            {data?.configuredPath ? 'Đang dùng đường dẫn tuỳ chỉnh' : 'Đang dùng mặc định'}
          </Descriptions.Item>
        </Descriptions>

        <Form layout="vertical" onFinish={() => saveMutation.mutate(path.trim() ? path.trim() : null)}>
          <Form.Item
            label="Đường dẫn tuỳ chỉnh (để trống = dùng mặc định)"
            help="Phải là đường dẫn tuyệt đối và ghi được, vd D:\\ptkd-storage"
          >
            <Input
              value={path}
              onChange={(e) => setPath(e.target.value)}
              placeholder={data?.defaultPath}
              data-testid="storage-path-input"
            />
          </Form.Item>
          <Space>
            <Button type="primary" htmlType="submit" loading={saveMutation.isPending} data-testid="storage-path-save">
              Lưu
            </Button>
            <Button
              onClick={() => { setPath(''); saveMutation.mutate(null); }}
              loading={saveMutation.isPending}
            >
              Về mặc định
            </Button>
          </Space>
        </Form>
      </Card>
    </div>
  );
};

export default SystemStoragePage;
