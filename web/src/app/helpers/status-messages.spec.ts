import { HttpErrorResponse } from '@angular/common/http';
import type { RequestDto } from '../models/request.dto';
import { bulkResultNotice, statusUpdateFailedNotice } from './status-messages';

const current: RequestDto = {
  id: 7,
  title: 'Refund missing #7',
  organizationName: 'Alpha Labs',
  status: 'Waiting',
  priority: 'High',
  assignedTo: 'agent01',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-02T00:00:00Z',
  rowVersion: 'AAAAAAAAB9E=',
};

describe('statusUpdateFailedNotice', () => {
  it('turns a 409 into a warning that names the current status in Hebrew', () => {
    const conflict = new HttpErrorResponse({ status: 409, error: { title: 'Concurrency conflict', current } });

    const notice = statusUpdateFailedNotice(7, conflict);

    expect(notice.type).toBe('warning');
    expect(notice.text).toContain('משתמש אחר');
    expect(notice.text).toContain('ממתינה');
  });

  it('shows any other failure as an error with the server message', () => {
    const invalid = new HttpErrorResponse({ status: 422, error: { detail: 'Status transition New -> Completed is not allowed.' } });

    expect(statusUpdateFailedNotice(7, invalid)).toEqual({
      type: 'error',
      text: 'Status transition New -> Completed is not allowed.',
    });
  });
});

describe('bulkResultNotice', () => {
  it('reports partial success with the reason for every failed item', () => {
    const notice = bulkResultNotice({
      succeeded: 1,
      failed: 2,
      results: [
        { id: 1, outcome: 'Updated', rowVersion: 'AAAAAAAAB9I=' },
        { id: 2, outcome: 'Conflict' },
        { id: 3, outcome: 'NotFound' },
      ],
    });

    expect(notice.type).toBe('warning');
    expect(notice.text).toContain('1 הצליחו, 2 נכשלו');
    expect(notice.text).toContain('#2: עודכנה ע״י משתמש אחר');
    expect(notice.text).toContain('#3: לא נמצאה');
  });
});
