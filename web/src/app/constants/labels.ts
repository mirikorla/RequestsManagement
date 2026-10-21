import type { Priority } from '../enums/priority';
import type { RequestStatus } from '../enums/request-status';

// Hebrew display texts. The API always uses the English enum values; only the UI translates them.

export const STATUS_LABELS: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה',
};

export const PRIORITY_LABELS: Record<Priority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה',
};
