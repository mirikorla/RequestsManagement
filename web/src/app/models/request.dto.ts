import type { Priority } from '../enums/priority';
import type { RequestStatus } from '../enums/request-status';

/** Mirrors RequestDto. rowVersion must be sent back unchanged on update (optimistic concurrency). */
export interface RequestDto {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: Priority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}
