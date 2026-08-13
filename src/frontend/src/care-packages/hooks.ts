import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useCompany } from '../auth/CompanyProvider';
import * as api from './carePackageApi';
import type {
  CreateCarePackageRequest,
  ApproveRejectRequest,
  CreatePaymentRequest,
} from './types';

export const useCarePackageRequests = (params?: Record<string, any>) => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['carePackageRequests', currentCompanyId, params],
    queryFn: () => api.listCarePackageRequests(currentCompanyId!, params),
    enabled: !!currentCompanyId,
  });
};

export const useCarePackageRequest = (id: number) => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['carePackageRequest', currentCompanyId, id],
    queryFn: () => api.getCarePackageRequest(currentCompanyId!, id),
    enabled: !!id && !!currentCompanyId,
  });
};

export const useCreateCarePackageRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: (data: CreateCarePackageRequest) => api.createCarePackageRequest(currentCompanyId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useSubmitCarePackageRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: (id: number) => api.submitCarePackageRequest(currentCompanyId!, id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', currentCompanyId, id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useApproveCarePackageRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: ApproveRejectRequest }) =>
      api.approveCarePackageRequest(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', currentCompanyId, variables.id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useRejectCarePackageRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: ApproveRejectRequest }) =>
      api.rejectCarePackageRequest(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', currentCompanyId, variables.id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useCreateCarePackagePayment = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreatePaymentRequest }) =>
      api.createCarePackagePayment(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', currentCompanyId, variables.id] });
    },
  });
};

export const useCarePackagePaymentStatus = (id: number, enabled: boolean = false) => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['carePackagePaymentStatus', currentCompanyId, id],
    queryFn: () => api.getCarePackagePaymentStatus(currentCompanyId!, id),
    enabled: !!id && !!currentCompanyId && enabled,
    refetchInterval: 5000,
  });
};

export const useActivateCarePackageRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: (id: number) => api.activateCarePackageRequest(currentCompanyId!, id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', currentCompanyId, id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};
