import axiosClient from '../api/axiosClient';

export interface SecurityAuditEventDto {
  id: number;
  actorUserId: number | null;
  actingAsUserId: number | null;
  targetUserId: number | null;
  companyId: number | null;
  eventCode: string;
  entityType: string;
  entityId: string | null;
  reason: string | null;
  correlationId: string;
  outcome: string;
  policyVersion: number | null;
  createdAt: string;
}

export interface SecurityAuditQueryParameters {
  fromUtc?: string;
  toUtc?: string;
  eventType?: string;
  actorUserId?: number;
  targetUserId?: number;
  entityType?: string;
  entityId?: string;
  correlationId?: string;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const getAuditEvents = async (
  params: SecurityAuditQueryParameters
): Promise<PagedResult<SecurityAuditEventDto>> => {
  const response = await axiosClient.get('/security/audit-events', {
    params: {
      fromUtc: params.fromUtc,
      toUtc: params.toUtc,
      eventType: params.eventType,
      actorUserId: params.actorUserId,
      targetUserId: params.targetUserId,
      entityType: params.entityType,
      entityId: params.entityId,
      correlationId: params.correlationId,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
  return response.data;
};
