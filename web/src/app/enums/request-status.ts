// Mirrors RequestsManagement.Core.Enums.RequestStatus. Enums travel as strings in JSON.
export const STATUSES = ['New', 'InProgress', 'Waiting', 'Completed'] as const;

export type RequestStatus = (typeof STATUSES)[number];
