export const getSanitizedErrorMessage = (error: unknown, fallback: string = 'An unexpected error occurred.'): string => {
  if (typeof error === 'object' && error !== null) {
    const e = error as any;
    if (e.response?.data?.detail) {
      return e.response.data.detail;
    }
    if (e.response?.data?.title) {
      return e.response.data.title;
    }
    if (e.message) {
      return e.message;
    }
  }
  return fallback;
};
