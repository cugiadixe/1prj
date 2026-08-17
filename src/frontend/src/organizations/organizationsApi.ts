/**
 * Organization management API — công ty, phòng ban, người dùng, gán công ty/phòng.
 * Endpoint backend (đã có sẵn), gác quyền ORGANIZATION_*_MANAGE (GLOBAL).
 */
import axiosClient from '../api/axiosClient';

// ── Types ────────────────────────────────────────────────────────────────────
export interface CompanyDto {
  id: number;
  companyCode: string;
  parentCompanyId: number | null;
  name: string;
  taxCode: string | null;
  isActive: boolean;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}
export interface CreateCompanyRequest {
  companyCode: string;
  parentCompanyId?: number | null;
  name: string;
  taxCode?: string | null;
}
export interface UpdateCompanyRequest extends CreateCompanyRequest {
  targetVersion: string;
}

export interface DepartmentDto {
  id: number;
  departmentCode: string;
  companyId: number;
  parentDepartmentId: number | null;
  name: string;
  isActive: boolean;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}
export interface CreateDepartmentRequest {
  departmentCode: string;
  companyId: number;
  parentDepartmentId?: number | null;
  name: string;
}
export interface UpdateDepartmentRequest {
  departmentCode: string;
  parentDepartmentId?: number | null;
  name: string;
  targetVersion: string;
}

export interface UserDto {
  id: number;
  employeeCode: string;
  fullName: string;
  email: string | null;
  employmentStatus: string;
  accountStatus: string;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}
export interface CreateUserRequest {
  employeeCode: string;
  fullName: string;
  email?: string | null;
  employmentStatus: string;
  accountStatus: string;
  initialCompanyId: number;
  initialDepartmentId: number;
  effectiveFrom: string;
  reason?: string | null;
}
export interface UpdateUserRequest {
  employeeCode: string;
  fullName: string;
  email?: string | null;
  employmentStatus: string;
  accountStatus: string;
  targetVersion: string;
}

// ── Companies ─────────────────────────────────────────────────────────────────
export async function listCompanies(): Promise<CompanyDto[]> {
  const { data } = await axiosClient.get<CompanyDto[]>('/organizations/companies');
  return data;
}
export async function createCompany(req: CreateCompanyRequest): Promise<CompanyDto> {
  const { data } = await axiosClient.post<CompanyDto>('/organizations/companies', req);
  return data;
}
export async function updateCompany(id: number, req: UpdateCompanyRequest): Promise<CompanyDto> {
  const { data } = await axiosClient.put<CompanyDto>(`/organizations/companies/${id}`, req);
  return data;
}
export async function setCompanyStatus(id: number, isActive: boolean, targetVersion: string): Promise<CompanyDto> {
  const { data } = await axiosClient.put<CompanyDto>(`/organizations/companies/${id}/status`, { isActive, targetVersion });
  return data;
}

// ── Departments ───────────────────────────────────────────────────────────────
export async function listDepartments(companyId: number): Promise<DepartmentDto[]> {
  const { data } = await axiosClient.get<DepartmentDto[]>('/organizations/departments', { params: { companyId } });
  return data;
}
export async function createDepartment(req: CreateDepartmentRequest): Promise<DepartmentDto> {
  const { data } = await axiosClient.post<DepartmentDto>('/organizations/departments', req);
  return data;
}
export async function updateDepartment(id: number, req: UpdateDepartmentRequest): Promise<DepartmentDto> {
  const { data } = await axiosClient.put<DepartmentDto>(`/organizations/departments/${id}`, req);
  return data;
}
export async function setDepartmentStatus(id: number, isActive: boolean, targetVersion: string): Promise<DepartmentDto> {
  const { data } = await axiosClient.put<DepartmentDto>(`/organizations/departments/${id}/status`, { isActive, targetVersion });
  return data;
}

// ── Users ─────────────────────────────────────────────────────────────────────
export async function listUsers(): Promise<UserDto[]> {
  const { data } = await axiosClient.get<UserDto[]>('/organizations/users');
  return data;
}
export async function createUser(req: CreateUserRequest): Promise<UserDto> {
  const { data } = await axiosClient.post<UserDto>('/organizations/users', req);
  return data;
}
export async function updateUser(id: number, req: UpdateUserRequest): Promise<UserDto> {
  const { data } = await axiosClient.put<UserDto>(`/organizations/users/${id}`, req);
  return data;
}
