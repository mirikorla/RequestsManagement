// Mirrors RequestsManagement.Core.Enums.BulkItemOutcome: what happened to one item of a bulk update.
export type BulkItemOutcome = 'Updated' | 'NotFound' | 'Conflict' | 'InvalidTransition';
