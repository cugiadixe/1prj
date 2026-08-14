import axiosClient from '../api/axiosClient';
import type {
  ApprovalAuthority,
  ApproverOption,
  CreateApprovalAuthorityRequest,
  OrgCompany,
  OrgDepartment,
} from './types';

const BASE = '/approval-authorities';

export async function listAuthorities(params: {
  companyId?: number;
  departmentId?: number;
  includeClosed?: boolean;
}): Promise<ApprovalAuthority[]> {
  const { data } = await axiosClient.get<ApprovalAuthority[]>(BASE, { params });
  return data;
}

export async function createAuthority(
  request: CreateApprovalAuthorityRequest,
): Promise<ApprovalAuthority> {
  const { data } = await axiosClient.post<ApprovalAuthority>(BASE, request);
  return data;
}

export async function closeAuthority(id: number, effectiveTo: string): Promise<ApprovalAuthority> {
  const { data } = await axiosClient.post<ApprovalAuthority>(`${BASE}/${id}/close`, { effectiveTo });
  return data;
}

export async function listCompanies(): Promise<OrgCompany[]> {
  const { data } = await axiosClient.get<OrgCompany[]>('/organizations/companies');
  return data;
}

export async function listDepartments(companyId?: number): Promise<OrgDepartment[]> {
  const { data } = await axiosClient.get<OrgDepartment[]>('/organizations/departments', {
    params: companyId ? { companyId } : {},
  });
  return data;
}

export async function listApproverOptions(
  companyId: number,
  departmentId?: number,
  search?: string,
): Promise<ApproverOption[]> {
  const { data } = await axiosClient.get<ApproverOption[]>(`${BASE}/approver-options`, {
    params: { companyId, departmentId, search: search || undefined },
  });
  return data;
}

export function getApprovalAuthorityErrorMessage(error: unknown): string {
  const codes: Record<string, string> = {
    AA_INVALID_LEVEL: 'Cấp thẩm quyền phải lớn hơn 0.',
    AA_INVALID_AMOUNT_RANGE: 'Ngưỡng tiền tối đa không được nhỏ hơn tối thiểu.',
    AA_INVALID_EFFECTIVE_RANGE: 'Ngày kết thúc hiệu lực phải sau ngày bắt đầu.',
    AA_COMPANY_NOT_FOUND: 'Không tìm thấy công ty.',
    AA_DEPARTMENT_NOT_FOUND: 'Không tìm thấy phòng ban.',
    AA_DEPARTMENT_COMPANY_MISMATCH: 'Phòng ban không thuộc công ty đã chọn.',
    AA_APPROVER_NOT_FOUND: 'Không tìm thấy người duyệt.',
    AA_APPROVER_INACTIVE: 'Người duyệt không ở trạng thái hoạt động.',
    AA_DELEGATOR_NOT_FOUND: 'Không tìm thấy người uỷ quyền gốc.',
    AA_PROCESS_NOT_FOUND: 'Không tìm thấy mã quy trình.',
    AA_NOT_FOUND: 'Không tìm thấy dòng thẩm quyền.',
  };
  try {
    const err = error as {
      response?: { status?: number; data?: { extensions?: Record<string, unknown>; detail?: string } };
    };
    if (err?.response?.status === 403) return 'Bạn không có quyền khai báo thẩm quyền phê duyệt.';
    const ext = err?.response?.data?.extensions;
    if (ext && typeof ext['errorCode'] === 'string') {
      const code = ext['errorCode'] as string;
      return err?.response?.data?.detail ?? codes[code] ?? 'Đã có lỗi xảy ra.';
    }
  } catch {
    // ignore
  }
  return 'Đã có lỗi xảy ra. Vui lòng thử lại.';
}
