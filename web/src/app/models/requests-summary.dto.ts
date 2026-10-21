import type { Priority } from '../enums/priority';
import type { RequestStatus } from '../enums/request-status';

/** Mirrors RequestsSummaryDto: counts by status and by priority (every value is always present). */
export interface RequestsSummaryDto {
  total: number;
  byStatus: Record<RequestStatus, number>;
  byPriority: Record<Priority, number>;
  generatedAt: string;
}
