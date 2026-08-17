import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useCompany } from '../auth/CompanyProvider';
import * as api from './cardsApi';
import type { CreateCardRequest } from './cardsApi';

export const useCards = () => {
  const { currentCompanyId } = useCompany();
  return useQuery({
    queryKey: ['cards', currentCompanyId],
    queryFn: () => api.getCards(currentCompanyId!),
    enabled: !!currentCompanyId,
  });
};

export const useCreateCard = () => {
  const queryClient = useQueryClient();
  const { currentCompanyId } = useCompany();
  return useMutation({
    mutationFn: (req: CreateCardRequest) => api.createCard(currentCompanyId!, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cards'] });
    },
  });
};
