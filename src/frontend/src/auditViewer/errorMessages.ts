export const getAuditViewerErrorMessage = (error: any): string => {
  if (error.response && error.response.data && error.response.data.title) {
    return error.response.data.title;
  }
  return 'Failed to load audit events.';
};
