import type { RequestStatus } from '../enums/request-status';

/** Mirrors StatusHistoryDto: one audit row of a status change. */
export interface StatusHistoryDto {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}
