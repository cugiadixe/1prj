import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../api/axiosClient';
import {
  getBusinessProcesses,
  searchDefinitions,
  getDefinitionById,
  createDefinition,
  updateDefinition,
  getVersionsByDefinition,
  createVersion,
  getVersionById,
  deleteVersion,
  createStep,
  updateStep,
  deleteStep,
  createApproverRule,
  publishVersion,
  activateVersion,
  retireVersion,
  getBindings,
  createBinding,
  updateBinding,
  updateApproverRule,
  deleteApproverRule,
  createCondition,
  deleteCondition,
} from './workflowApi';

vi.mock('../api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

const mockAxios = vi.mocked(axiosClient);

describe('workflowApi', () => {
  beforeEach(() => vi.clearAllMocks());

  it('getBusinessProcesses calls GET /workflows/processes', async () => {
    mockAxios.get.mockResolvedValue({ data: [] });
    await getBusinessProcesses();
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/processes');
  });

  it('searchDefinitions calls GET /workflows/definitions', async () => {
    mockAxios.get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
    await searchDefinitions({ processCode: 'CUST', page: 2 });
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/definitions', {
      params: { processCode: 'CUST', isActive: undefined, page: 2, pageSize: 20 },
    });
  });

  it('getDefinitionById calls GET /workflows/definitions/:id', async () => {
    mockAxios.get.mockResolvedValue({ data: { id: 1 } });
    await getDefinitionById(1);
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/definitions/1');
  });

  it('createDefinition calls POST /workflows/definitions', async () => {
    const req = { definitionCode: 'WF1', definitionName: 'Test', processCode: 'CUST' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createDefinition(req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/definitions', req);
  });

  it('updateDefinition calls PUT /workflows/definitions/:id', async () => {
    const req = { definitionName: 'Updated', targetVersion: 'AA' };
    mockAxios.put.mockResolvedValue({ data: { id: 1 } });
    await updateDefinition(1, req);
    expect(mockAxios.put).toHaveBeenCalledWith('/workflows/definitions/1', req);
  });

  it('getVersionsByDefinition calls GET /workflows/definitions/:id/versions', async () => {
    mockAxios.get.mockResolvedValue({ data: [] });
    await getVersionsByDefinition(1);
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/definitions/1/versions');
  });

  it('createVersion calls POST /workflows/definitions/:id/versions', async () => {
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createVersion(1);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/definitions/1/versions');
  });

  it('getVersionById calls GET /workflows/versions/:id', async () => {
    mockAxios.get.mockResolvedValue({ data: { id: 1 } });
    await getVersionById(1);
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/versions/1');
  });

  it('deleteVersion calls DELETE /workflows/versions/:id', async () => {
    mockAxios.delete.mockResolvedValue({});
    await deleteVersion(1);
    expect(mockAxios.delete).toHaveBeenCalledWith('/workflows/versions/1');
  });

  it('createStep calls POST /workflows/versions/:id/steps', async () => {
    const req = { stepName: 'S1', stepOrder: 1, isRequired: true };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createStep(1, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/versions/1/steps', req);
  });

  it('updateStep calls PUT /workflows/steps/:id', async () => {
    const req = { stepName: 'S1', stepOrder: 1, isRequired: true, targetVersion: 'AA' };
    mockAxios.put.mockResolvedValue({ data: { id: 1 } });
    await updateStep(1, req);
    expect(mockAxios.put).toHaveBeenCalledWith('/workflows/steps/1', req);
  });

  it('deleteStep calls DELETE /workflows/steps/:id', async () => {
    mockAxios.delete.mockResolvedValue({});
    await deleteStep(1);
    expect(mockAxios.delete).toHaveBeenCalledWith('/workflows/steps/1');
  });

  it('createApproverRule calls POST /workflows/steps/:id/approver-rules', async () => {
    const req = { approverSourceType: 'ROLE', approverSourceValue: 'ADMIN', priority: 1 };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createApproverRule(1, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/steps/1/approver-rules', req);
  });

  it('publishVersion calls POST /workflows/versions/:id/publish', async () => {
    const req = { effectiveFrom: '2026-01-01', targetVersion: 'AA' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await publishVersion(1, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/versions/1/publish', req);
  });

  it('activateVersion calls POST /workflows/versions/:id/activate', async () => {
    const req = { targetVersion: 'AA' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await activateVersion(1, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/versions/1/activate', req);
  });

  it('retireVersion calls POST /workflows/versions/:id/retire', async () => {
    const req = { targetVersion: 'AA' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await retireVersion(1, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/versions/1/retire', req);
  });

  it('getBindings calls GET /workflows/bindings', async () => {
    mockAxios.get.mockResolvedValue({ data: [] });
    await getBindings('CUST');
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/bindings', {
      params: { processCode: 'CUST' },
    });
  });

  it('createBinding calls POST /workflows/bindings', async () => {
    const req = {
      workflowVersionId: 1,
      processCode: 'CUST',
      scopeType: 'GLOBAL',
      priority: 1,
      effectiveFrom: '2026-01-01',
    };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createBinding(req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/bindings', req);
  });

  it('updateBinding calls PUT /workflows/bindings/:id', async () => {
    const req = { effectiveFrom: '2026-01-01', priority: 2, targetVersion: 'AA' };
    mockAxios.put.mockResolvedValue({ data: { id: 1 } });
    await updateBinding(1, req);
    expect(mockAxios.put).toHaveBeenCalledWith('/workflows/bindings/1', req);
  });

  // Trước đây endpoint sửa/xoá luật người duyệt CHƯA được xây, và test này đóng băng hiện trạng đó.
  // Nay đã có (Nhóm 1): tài liệu quản trị cho phép sửa cấu hình ở trạng thái NHÁP
  // ("Only DRAFT configuration is editable"), và backend chặn WF_VERSION_NOT_DRAFT nếu
  // phiên bản đã xuất bản. Không có 2 endpoint này thì gõ sai một luật phải xoá cả bước.
  it('updateApproverRule calls PUT /workflows/approver-rules/:id', async () => {
    const req = { approverSourceType: 'ROLE', approverSourceValue: 'MANAGER', priority: 1 };
    mockAxios.put.mockResolvedValue({ data: { id: 7 } });
    await updateApproverRule(7, req);
    expect(mockAxios.put).toHaveBeenCalledWith('/workflows/approver-rules/7', req);
  });

  it('deleteApproverRule calls DELETE /workflows/approver-rules/:id', async () => {
    mockAxios.delete.mockResolvedValue({ data: undefined });
    await deleteApproverRule(7);
    expect(mockAxios.delete).toHaveBeenCalledWith('/workflows/approver-rules/7');
  });

  // Trước đây điều kiện chỉ đọc được, vì KHÔNG có code nào đánh giá chúng lúc chạy — cho tạo
  // sẽ là bẫy (admin tưởng đã đặt luật nhưng hệ bỏ qua). Nhóm 2 đã xây bộ đánh giá, nên nay
  // tạo/xoá điều kiện là hợp lệ (chỉ trên bản NHÁP, và trường phải nằm trong danh mục DEV khai báo).
  it('createCondition calls POST /workflows/versions/:id/conditions', async () => {
    const req = { fieldCode: 'TotalAmount', operator: 'GT', value: '50000000' };
    mockAxios.post.mockResolvedValue({ data: { id: 3 } });
    await createCondition(5, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/versions/5/conditions', req);
  });

  it('deleteCondition calls DELETE /workflows/conditions/:id', async () => {
    mockAxios.delete.mockResolvedValue({ data: undefined });
    await deleteCondition(3);
    expect(mockAxios.delete).toHaveBeenCalledWith('/workflows/conditions/3');
  });
});
