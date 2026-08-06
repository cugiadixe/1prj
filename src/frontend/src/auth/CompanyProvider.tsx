import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { useAuth } from './AuthProvider';
import { apiFetchMyCompanies } from './authApi';
import type { UserCompanyDto } from './authApi';

interface CompanyContextType {
    companies: UserCompanyDto[];
    currentCompanyId: number | null;
    isLoading: boolean;
    switchCompany: (companyId: number) => void;
}

const CompanyContext = createContext<CompanyContextType | undefined>(undefined);

export function CompanyProvider({ children }: { children: ReactNode }) {
    const { isAuthenticated, mustChangePassword, refreshPermissions } = useAuth();
    const [companies, setCompanies] = useState<UserCompanyDto[]>([]);
    const [currentCompanyId, setCurrentCompanyId] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(false);

    useEffect(() => {
        if (!isAuthenticated || mustChangePassword) {
            setCompanies([]);
            setCurrentCompanyId(null);
            return;
        }

        let isMounted = true;

        async function fetchCompanies() {
            setIsLoading(true);
            try {
                const response = await apiFetchMyCompanies();
                if (!isMounted) return;

                setCompanies(response.companies);

                if (response.companies.length === 1) {
                    // Exactly one company → auto-select (Phase 1B.1-M rule)
                    setCurrentCompanyId(response.companies[0].companyId);
                } else {
                    // Zero or multiple companies → require manual selection
                    setCurrentCompanyId(null);
                }
            } catch {
                // Fetch failed (e.g. 401 after interceptor exhausted retry).
                // Phase M: do not log auth/company/permission payloads.
                // The axios interceptor in AuthProvider will have called clearAuth()
                // on unrecoverable 401, causing isAuthenticated → false and
                // triggering the cleanup effect above.
                if (isMounted) {
                    setCompanies([]);
                    setCurrentCompanyId(null);
                }
            } finally {
                if (isMounted) setIsLoading(false);
            }
        }

        fetchCompanies();

        return () => {
            isMounted = false;
        };
    }, [isAuthenticated, mustChangePassword]);

    const switchCompany = (companyId: number) => {
        if (companies.some(c => c.companyId === companyId)) {
            setCurrentCompanyId(companyId);
            refreshPermissions(companyId);
        }
    };

    // Auto-refresh permissions on initial load if we select a default company
    // To prevent loops, we track if we've already done the initial permission fetch.
    const [initialFetchDone, setInitialFetchDone] = useState(false);

    useEffect(() => {
        if (currentCompanyId !== null && !initialFetchDone) {
            refreshPermissions(currentCompanyId);
            setInitialFetchDone(true);
        }
    }, [currentCompanyId, initialFetchDone, refreshPermissions]);

    // Reset initial fetch on logout
    useEffect(() => {
        if (!isAuthenticated) {
            setInitialFetchDone(false);
        }
    }, [isAuthenticated]);

    return (
        <CompanyContext.Provider value={{ companies, currentCompanyId, isLoading, switchCompany }}>
            {children}
        </CompanyContext.Provider>
    );
}

export function useCompany() {
    const context = useContext(CompanyContext);
    if (context === undefined) {
        throw new Error('useCompany must be used within a CompanyProvider');
    }
    return context;
}
