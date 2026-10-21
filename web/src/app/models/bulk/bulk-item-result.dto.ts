import type { BulkItemOutcome } from '../../enums/bulk-item-outcome';

/** Mirrors BulkItemResultDto: the outcome of one item. rowVersion is set only when it was updated. */
export interface BulkItemResultDto {
  id: number;
  outcome: BulkItemOutcome;
  rowVersion?: string | null;
  error?: string | null;
}
