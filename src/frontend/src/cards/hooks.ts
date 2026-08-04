import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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
  return useQuery({
    queryKey: ['cardReprintRequests', params],
    queryFn: () => api.getCardReprintRequests(params),
  });
};

export const useCardReprintRequest = (id: number) => {
  return useQuery({
    queryKey: ['cardReprintRequest', id],
    queryFn: () => api.getCardReprintRequest(id),
    enabled: !!id,
  });
};

export const useCreateCardReprintRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCardReprintRequest) => api.createCardReprintRequest(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useSubmitCardReprintRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: SubmitCardReprintRequest }) =>
      api.submitCardReprintRequest(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useApproveCardReprintRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: ApproveCardReprintRequest }) =>
      api.approveCardReprintRequest(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useRejectCardReprintRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: RejectCardReprintRequest }) =>
      api.rejectCardReprintRequest(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequests'] });
    },
  });
};

export const useCreatePaymentForCardReprint = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: CreateCardReprintPaymentRequest }) =>
      api.createPaymentForCardReprint(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', variables.id] });
    },
  });
};

export const useCardReprintPaymentStatus = (id: number, enabled: boolean = false) => {
  return useQuery({
    queryKey: ['cardReprintPaymentStatus', id],
    queryFn: () => api.getCardReprintPaymentStatus(id),
    enabled: !!id && enabled,
    refetchInterval: 5000,
  });
};

export const useMarkCardPrinted = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: MarkCardPrintedRequest }) =>
      api.markCardPrinted(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', variables.id] });
    },
  });
};

export const useMarkCardReleased = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: MarkCardReleasedRequest }) =>
      api.markCardReleased(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cardReprintRequest', variables.id] });
    },
  });
};
