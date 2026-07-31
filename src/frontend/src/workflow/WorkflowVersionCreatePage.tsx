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
        message="You do not have permission to create workflow versions."
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
          Create New Version for: {definition.definitionName}
        </Title>
        <Button>
          <Link to={`/workflow/definitions/${defId}`}>Back to Definition</Link>
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
          A new DRAFT version will be created for definition{' '}
          <strong>{definition.definitionCode}</strong>. You can then add steps and approver
          rules before publishing.
        </Typography.Paragraph>
        <Space>
          <Button
            type="primary"
            onClick={() => createMutation.mutate()}
            loading={createMutation.isPending}
            data-testid="submit-create-version"
          >
            Create Draft Version
          </Button>
          <Button>
            <Link to={`/workflow/definitions/${defId}`}>Cancel</Link>
          </Button>
        </Space>
      </Card>
    </div>
  );
};

export default WorkflowVersionCreatePage;
