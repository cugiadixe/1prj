import axiosClient from '../api/axiosClient';
import type { CreateCustomerCarePackageRequest, CustomerCarePackage } from './types';

const BASE = '/customer-care-packages';

export async function listByCustomer(customerId: number): Promise<CustomerCarePackage[]> {
  const { data } = await axiosClient.get<CustomerCarePackage[]>(BASE, { params: { customerId } });
  return data;
}

export async function listByGrave(graveId: number): Promise<CustomerCarePackage[]> {
  const { data } = await axiosClient.get<CustomerCarePackage[]>(BASE, { params: { graveId } });
  return data;
}

export async function createPackage(
  request: CreateCustomerCarePackageRequest,
  companyId?: number | null,
): Promise<CustomerCarePackage> {
  // X-Company-Id để backend biết công ty nào — quyết định có chạy quy trình phê duyệt hay không.
  const headers = companyId != null ? { 'X-Company-Id': String(companyId) } : undefined;
  const { data } = await axiosClient.post<CustomerCarePackage>(BASE, request, { headers });
  return data;
}

export async function assignGrave(id: number, graveId: number): Promise<CustomerCarePackage> {
  const { data } = await axiosClient.post<CustomerCarePackage>(`${BASE}/${id}/assign-grave`, { graveId });
  return data;
}

export async function cancelPackage(id: number): Promise<CustomerCarePackage> {
  const { data } = await axiosClient.post<CustomerCarePackage>(`${BASE}/${id}/cancel`);
  return data;
}

export function getCcpErrorMessage(error: unknown): string {
  const codes: Record<string, string> = {
    CCP_GRAVE_NOT_OWNED: 'Phần mộ không thuộc sở hữu của khách hàng này.',
    CCP_COT_COUNT_MISMATCH: 'Số cốt của gói không khớp với số cốt của mộ.',
    CCP_DUPLICATE_ON_GRAVE: 'Mộ này đã có một gói cùng loại đang hiệu lực.',
    CCP_GRAVE_NOT_FOUND: 'Không tìm thấy phần mộ.',
    CCP_CUSTOMER_NOT_FOUND: 'Không tìm thấy khách hàng.',
    CCP_SERVICE_TYPE_NOT_FOUND: 'Không tìm thấy gói chăm sóc.',
    CCP_NOT_FOUND: 'Không tìm thấy gói chăm sóc của khách.',
    CCP_CANCELLED: 'Gói đã hủy, không thể gán mộ.',
    CCP_INVALID_COT_COUNT: 'Số cốt phải lớn hơn 0.',
    // Chặn khi chưa khai báo quy trình phê duyệt (Nhóm 0).
    CCP_COMPANY_CONTEXT_REQUIRED:
      'Chưa xác định công ty làm việc nên không xác định được quy trình phê duyệt. Vui lòng chọn công ty rồi thử lại.',
    CCP_APPROVAL_NOT_CONFIGURED:
      'Quy trình phê duyệt gán gói dịch vụ chưa được cấu hình cho công ty này. Vui lòng liên hệ quản trị.',
    // Lỗi từ engine quy trình có thể nổi lên khi tạo gói.
    WF_NO_VALID_BINDING:
      'Quy trình phê duyệt chưa được khai báo liên kết đang hiệu lực. Vui lòng liên hệ quản trị.',
    WF_NO_ASSIGNEE_FOR_STEP:
      'Chưa xác định được người duyệt. Vui lòng kiểm tra cấu hình thẩm quyền phê duyệt.',
    WF_NO_EXECUTION_HANDLER:
      'Quy trình này chưa có bộ xử lý thực thi nên chưa dùng được. Vui lòng báo bộ phận CNTT.',
    WF_BINDING_AMBIGUOUS:
      'Quy trình có nhiều liên kết trùng hạng — lỗi cấu hình. Vui lòng liên hệ quản trị.',
  };
  try {
    const err = error as {
      response?: { status?: number; data?: { extensions?: Record<string, unknown>; detail?: string } };
    };
    if (err?.response?.status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
    const ext = err?.response?.data?.extensions;
    if (ext && typeof ext['errorCode'] === 'string') {
      const code = ext['errorCode'] as string;
      // ưu tiên detail cụ thể từ server (vd thông báo số cốt kèm số liệu)
      return err?.response?.data?.detail ?? codes[code] ?? 'Đã có lỗi xảy ra.';
    }
  } catch {
    // ignore
  }
  return 'Đã có lỗi xảy ra. Vui lòng thử lại.';
}
