export interface CreateCustomerMergeRequest {
  sourceCustomerId: number;
  targetCustomerId: number;
  survivorshipPayload: string;
  sourceRowVersionSnapshot: string;
  targetRowVersionSnapshot: string;
  candidates: CustomerMergeCandidateInput[];
}

export interface CustomerMergeCandidateInput {
  candidateCustomerId: number;
  matchType: string;
  matchConfidence: number | null;
  snapshotPayload: string | null;
}

export interface CustomerMergeCandidate {
  candidateCustomerId: number;
  matchType: string;
  matchConfidence: number | null;
  snapshotPayload: string | null;
}

export interface CustomerMergeRequestDto {
  id: string;
  sourceCustomerId: number;
  targetCustomerId: number;
  requesterId: number;
  requestStatus: string;
  survivorshipPayload: string;
  sourceRowVersionSnapshot: string;
  targetRowVersionSnapshot: string;
  workflowInstanceId: number | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
  candidates: CustomerMergeCandidate[];
}

export interface MergeDuplicateSearchParams {
  cccd?: string;
  phone?: string;
}

export interface MergeRequestListParams {
  page?: number;
  pageSize?: number;
}
