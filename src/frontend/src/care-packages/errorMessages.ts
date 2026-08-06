import axios from 'axios';

export const isPermissionDenied = (error: unknown): boolean => {
  if (axios.isAxiosError(error) && error.response?.status === 403) {
    return true;
  }
  return false;
};

export const getErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 400) {
      return error.response.data?.detail || error.response.data?.title || 'Bad Request';
    }
    if (error.response?.status === 404) {
      return 'Care package request not found';
    }
    if (error.response?.status === 409) {
      return error.response.data?.detail || error.response.data?.title || 'Invalid state transition';
    }
    return error.response?.data?.message || error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred.';
};
