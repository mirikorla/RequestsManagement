import type { BulkItemResultDto } from './bulk-item-result.dto';

/** Mirrors BulkUpdateStatusResultDto: partial success, with a result per item. */
export interface BulkUpdateStatusResultDto {
  succeeded: number;
  failed: number;
  results: BulkItemResultDto[];
}
