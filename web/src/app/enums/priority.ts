// Mirrors RequestsManagement.Core.Enums.Priority.
export const PRIORITIES = ['Low', 'Medium', 'High'] as const;

export type Priority = (typeof PRIORITIES)[number];
