import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as api from './carePackageApi';
import type {
  CreateCarePackageRequest,
  ApproveRejectRequest,
  CreatePaymentRequest,
} from './types';

export const useCarePackageRequests = (params?: Record<string, any>) => {
  return useQuery({
    queryKey: ['carePackageRequests', params],
    queryFn: () => api.listCarePackageRequests(params),
  });
};

export const useCarePackageRequest = (id: number) => {
  return useQuery({
    queryKey: ['carePackageRequest', id],
    queryFn: () => api.getCarePackageRequest(id),
    enabled: !!id,
  });
};

export const useCreateCarePackageRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCarePackageRequest) => api.createCarePackageRequest(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useSubmitCarePackageRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api.submitCarePackageRequest(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useApproveCarePackageRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: ApproveRejectRequest }) =>
      api.approveCarePackageRequest(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useRejectCarePackageRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: ApproveRejectRequest }) =>
      api.rejectCarePackageRequest(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};

export const useCreateCarePackagePayment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreatePaymentRequest }) =>
      api.createCarePackagePayment(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', variables.id] });
    },
  });
};

export const useCarePackagePaymentStatus = (id: number, enabled: boolean = false) => {
  return useQuery({
    queryKey: ['carePackagePaymentStatus', id],
    queryFn: () => api.getCarePackagePaymentStatus(id),
    enabled: !!id && enabled,
    refetchInterval: 5000,
  });
};

export const useActivateCarePackageRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api.activateCarePackageRequest(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['carePackageRequest', id] });
      queryClient.invalidateQueries({ queryKey: ['carePackageRequests'] });
    },
  });
};
