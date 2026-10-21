import { HttpErrorResponse } from '@angular/common/http';
import { STATUS_LABELS } from '../constants/labels';
import type { BulkItemOutcome } from '../enums/bulk-item-outcome';
import type { BulkUpdateStatusResultDto } from '../models/bulk/bulk-update-status-result.dto';
import type { RequestDto } from '../models/request.dto';
import { describeError } from './describe-error';

/** A message shown above the table after a status change. */
export interface Notice {
  type: 'success' | 'warning' | 'error';
  text: string;
}

const BULK_OUTCOME_LABELS: Record<BulkItemOutcome, string> = {
  Updated: 'עודכנה',
  NotFound: 'לא נמצאה',
  Conflict: 'עודכנה ע״י משתמש אחר',
  InvalidTransition: 'מעבר סטטוס אינו מותר',
};

export function statusUpdatedNotice(updated: RequestDto): Notice {
  return {
    type: 'success',
    text: `פנייה ${updated.id} עודכנה לסטטוס "${STATUS_LABELS[updated.status]}".`,
  };
}

/** 409 gets a specific explanation with the current status; anything else falls back to the server's message. */
export function statusUpdateFailedNotice(requestId: number, error: unknown): Notice {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    const current = (error.error as { current?: RequestDto } | null)?.current;
    const now = current ? ` הסטטוס הנוכחי: "${STATUS_LABELS[current.status]}".` : '';
    return {
      type: 'warning',
      text: `פנייה ${requestId} עודכנה בינתיים על ידי משתמש אחר, והשינוי שלך לא נשמר.${now} הנתונים רועננו — ניתן לנסות שוב.`,
    };
  }
  return { type: 'error', text: describeError(error) };
}

export function bulkResultNotice(result: BulkUpdateStatusResultDto): Notice {
  const failures = result.results
    .filter((r) => r.outcome !== 'Updated')
    .map((r) => `#${r.id}: ${BULK_OUTCOME_LABELS[r.outcome]}`);
  return {
    type: result.failed === 0 ? 'success' : 'warning',
    text:
      `עדכון מרוכז: ${result.succeeded} הצליחו, ${result.failed} נכשלו.` +
      (failures.length ? ` ${failures.join(' · ')}` : ''),
  };
}

export function errorNotice(error: unknown): Notice {
  return { type: 'error', text: describeError(error) };
}
