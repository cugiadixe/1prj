import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Spin, Alert, Card, Tag } from 'antd';
import axiosClient from '../api/axiosClient';

interface HealthEntry {
  name: string;
  status: string;
  description: string | null;
  duration: string;
}

interface HealthResponse {
  status: string;
  entries: HealthEntry[];
}

const fetchSystemHealth = async (): Promise<HealthResponse> => {
  const { data } = await axiosClient.get('/health');
  // Support both JSON response and plain-text legacy response
  if (typeof data === 'string') {
    return { status: data, entries: [] };
  }
  return data as HealthResponse;
};

const SystemHealth: React.FC = () => {
  const { data, error, isLoading } = useQuery({
    queryKey: ['systemHealth'],
    queryFn: fetchSystemHealth,
    retry: false
  });

  if (isLoading) return <Spin size="large" data-testid="loading-spinner" />;

  if (error) {
    return (
      <Alert
        message="System Offline or Error"
        description={(error as Error).message}
        type="error"
        showIcon
      />
    );
  }

  return (
    <Card title="System Health" style={{ width: 400 }}>
      <p>
        Overall Status:{' '}
        <Tag color={data?.status === 'Healthy' ? 'green' : 'red'}>
          {data?.status}
        </Tag>
      </p>
      {data?.entries && data.entries.length > 0 && (
        <div>
          <p style={{ fontWeight: 'bold', marginBottom: 8 }}>Components:</p>
          {data.entries.map((entry) => (
            <p key={entry.name}>
              {entry.name}:{' '}
              <Tag color={entry.status === 'Healthy' ? 'green' : 'red'}>
                {entry.status}
              </Tag>
              <span style={{ color: '#888', fontSize: 12 }}>
                ({entry.duration})
              </span>
            </p>
          ))}
        </div>
      )}
    </Card>
  );
};

export default SystemHealth;
