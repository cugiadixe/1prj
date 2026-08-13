import React, { useState } from 'react';
import { Alert, Button, Card, Space, Spin, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { createVersion, getDefinitionById } from './workflowApi';
import { getErrorMessage, isPermissionDenied } from './errorMessages';

const { Title } = Typography;

const WorkflowVersionCreatePage: React.FC = () => {
  const { definitionId } = useParams<{ definitionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const defId = Number(definitionId);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const { data: definition, isLoading, error: fetchError } = useQuery({
    queryKey: ['workflow-definition', defId],
    queryFn: () => getDefinitionById(defId),
    enabled: !isNaN(defId),
  });

  const createMutation = useMutation({
    mutationFn: () => createVersion(defId),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['workflow-versions', defId] });
      navigate(`/workflow/definitions/${defId}/versions/${result.id}`);
    },
    onError: (err) => {
      setSubmitError(getErrorMessage(err));
    },
  });

  if (isLoading) return <Spin data-testid="version-create-loading" />;

  if (isPermissionDenied(fetchError)) {
    return (
      <Alert
        type="error"
        message="Bạn không có quyền tạo phiên bản quy trình."
        data-testid="permission-denied"
      />
    );
  }

  if (fetchError) {
    return (
      <Alert
        type="error"
        message={getErrorMessage(fetchError)}
        data-testid="version-create-fetch-error"
      />
    );
  }

  if (!definition) return null;

  return (
    <div data-testid="workflow-version-create-page">
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>
          Tạo phiên bản mới cho: {definition.definitionName}
        </Title>
        <Button>
          <Link to={`/workflow/definitions/${defId}`}>Quay lại định nghĩa</Link>
        </Button>
      </Space>

      {submitError && (
        <Alert
          type="error"
          message={submitError}
          closable
          onClose={() => setSubmitError(null)}
          style={{ marginBottom: 16 }}
          data-testid="create-version-error"
        />
      )}

      <Card>
        <Typography.Paragraph>
          Một phiên bản NHÁP mới sẽ được tạo cho định nghĩa{' '}
          <strong>{definition.definitionCode}</strong>. Sau đó bạn có thể thêm các bước và quy tắc
          phê duyệt trước khi xuất bản.
        </Typography.Paragraph>
        <Space>
          <Button
            type="primary"
            onClick={() => createMutation.mutate()}
            loading={createMutation.isPending}
            data-testid="submit-create-version"
          >
            Tạo phiên bản nháp
          </Button>
          <Button>
            <Link to={`/workflow/definitions/${defId}`}>Hủy</Link>
          </Button>
        </Space>
      </Card>
    </div>
  );
};

export default WorkflowVersionCreatePage;
