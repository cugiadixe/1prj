import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useCompany } from '../auth/CompanyProvider';
import * as api from './cardReprintApi';
import type {
  CreateCardReprintRequest,
  SubmitCardReprintRequest,
  ApproveCardReprintRequest,
  RejectCardReprintRequest,
  CreateCardReprintPaymentRequest,
  MarkCardPrintedRequest,
  MarkCardReleasedRequest,
} from './types';

export const useCardReprintRequests = (params?: Record<string, any>) => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['cardReprintRequests', currentCompanyId, params],
    queryFn: () => api.getCardReprintRequests(currentCompanyId!, params),
    enabled: !!currentCompanyId,
  });
};

export const useCardReprintRequest = (id: number) => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['cardReprintRequest', currentCompanyId, id],
    queryFn: () => api.getCardReprintRequest(currentCompanyId!, id),
    enabled: !!id && !!currentCompanyId,
  });
};

export const useCreateCardReprintRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: (data: CreateCardReprintRequest) => api.createCardReprintRequest(currentCompanyId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useSubmitCardReprintRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: SubmitCardReprintRequest }) =>
      api.submitCardReprintRequest(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', currentCompanyId, variables.id] });
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useApproveCardReprintRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: ApproveCardReprintRequest }) =>
      api.approveCardReprintRequest(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', currentCompanyId, variables.id] });
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useRejectCardReprintRequest = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: RejectCardReprintRequest }) =>
      api.rejectCardReprintRequest(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', currentCompanyId, variables.id] });
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useCreatePaymentForCardReprint = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreateCardReprintPaymentRequest }) =>
      api.createPaymentForCardReprint(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', currentCompanyId, variables.id] });
    },
  });
};

export const useCardReprintPaymentStatus = (id: number, enabled: boolean = false) => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['cardReprintPaymentStatus', currentCompanyId, id],
    queryFn: () => api.getCardReprintPaymentStatus(currentCompanyId!, id),
    enabled: !!id && enabled && !!currentCompanyId,
    refetchInterval: 5000,
  });
};

export const useMarkCardPrinted = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: MarkCardPrintedRequest }) =>
      api.markCardPrinted(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', currentCompanyId, variables.id] });
    },
  });
};

export const useMarkCardReleased = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: MarkCardReleasedRequest }) =>
      api.markCardReleased(currentCompanyId!, id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', currentCompanyId, variables.id] });
    },
  });
};
