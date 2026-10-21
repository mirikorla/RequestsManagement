import type { RequestStatus } from '../enums/request-status';

/** Mirrors UpdateStatusDto: the body of PATCH /api/requests/{id}/status. */
export interface UpdateStatusDto {
  status: RequestStatus;
  rowVersion: string;
  changedBy: string;
}
