import { createContext } from 'react';
import type { UserCompanyDto } from './authApi';

export interface CompanyContextType {
    companies: UserCompanyDto[];
    currentCompanyId: number | null;
    isLoading: boolean;
    switchCompany: (companyId: number) => void;
}

/**
 * Tách đối tượng context ra file riêng để hook kiểm quyền đọc được công ty đang chọn mà KHÔNG
 * tạo vòng import: CompanyProvider vốn đã import useAuth từ AuthProvider, nên nếu AuthProvider
 * import ngược lại CompanyProvider thì thành vòng.
 */
export const CompanyContext = createContext<CompanyContextType | undefined>(undefined);
