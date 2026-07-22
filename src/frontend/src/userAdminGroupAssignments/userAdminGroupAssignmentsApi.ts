import axios from 'axios';

export interface UserAdminGroupAssignmentDto {
  id: number;
  userId: number;
  adminGroupId: number;
  groupCode: string;
  groupName: string;
  assignmentStatus: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  rowVersion: string;
}

export interface CreateUserAdminGroupAssignmentRequest {
  adminGroupId: number;
  effectiveFrom: string;
  effectiveTo: string | null;
}

export interface DeactivateAssignmentRequest {
  rowVersion: string;
}

const BASE_URL = '/api/v2/security';

export const userAdminGroupAssignmentsApi = {
  getUserAdminGroupAssignments: async (userId: number): Promise<UserAdminGroupAssignmentDto[]> => {
    const response = await axios.get<UserAdminGroupAssignmentDto[]>(`${BASE_URL}/users/${userId}/admin-group-assignments`);
    return response.data;
  },

  assignAdminGroupToUser: async (userId: number, request: CreateUserAdminGroupAssignmentRequest, currentCompanyId?: number): Promise<UserAdminGroupAssignmentDto> => {
    const headers = currentCompanyId ? { 'X-Company-Id': currentCompanyId.toString() } : undefined;
    const response = await axios.post<UserAdminGroupAssignmentDto>(`${BASE_URL}/users/${userId}/admin-group-assignments`, request, { headers });
    return response.data;
  },

  deactivateUserAdminGroupAssignment: async (userId: number, assignmentId: number, request: DeactivateAssignmentRequest, currentCompanyId?: number): Promise<void> => {
    const headers = currentCompanyId ? { 'X-Company-Id': currentCompanyId.toString() } : undefined;
    await axios.delete(`${BASE_URL}/users/${userId}/admin-group-assignments/${assignmentId}`, { data: request, headers });
  }
};
