import type { RequestStatus } from '../../enums/request-status';
import type { BulkItemDto } from './bulk-item.dto';

/** Mirrors BulkUpdateStatusDto: the body of POST /api/requests/status/bulk (up to 100 items). */
export interface BulkUpdateStatusDto {
  status: RequestStatus;
  changedBy: string;
  items: BulkItemDto[];
}
