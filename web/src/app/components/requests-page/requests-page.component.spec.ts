import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, TestRequest } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { PagedResultDto } from '../../models/paged-result.dto';
import type { RequestDto } from '../../models/request.dto';
import { RequestsPageComponent } from './requests-page.component';

const row: RequestDto = {
  id: 1,
  title: 'Refund missing #1',
  organizationName: 'Alpha Labs',
  status: 'New',
  priority: 'High',
  assignedTo: 'agent01',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  rowVersion: 'AAAAAAAAB9E=',
};

const onePage = (rows: RequestDto[]): PagedResultDto<RequestDto> => ({
  items: rows,
  page: 1,
  pageSize: 20,
  totalCount: rows.length,
  totalPages: 1,
});

const summary = {
  total: 1,
  byStatus: { New: 1, InProgress: 0, Waiting: 0, Completed: 0 },
  byPriority: { Low: 0, Medium: 0, High: 1 },
  generatedAt: '2026-01-01T00:00:00Z',
};

describe('RequestsPageComponent', () => {
  let fixture: ComponentFixture<RequestsPageComponent>;
  let http: HttpTestingController;
  let page: HTMLElement;

  const searches = () => http.match((r) => r.url === '/api/requests');

  async function render() {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  /** Opens the page and answers the first list and summary requests with one "New" request. */
  async function openWithOneRow() {
    fixture = TestBed.createComponent(RequestsPageComponent);
    page = fixture.nativeElement;
    await render();
    http.expectOne('/api/requests/summary').flush(summary);
    const [first] = searches();
    first.flush(onePage([row]));
    await render();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  it('loads one page from the server, with paging and sorting sent as parameters', async () => {
    fixture = TestBed.createComponent(RequestsPageComponent);
    await render();

    const [request] = searches();
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.get('sortBy')).toBe('CreatedAt');
    expect(request.request.params.get('sortDirection')).toBe('Desc');
  });

  it('cancels the previous request when the query changes before it answered (switchMap)', async () => {
    await openWithOneRow();
    const titleHeader = [...page.querySelectorAll<HTMLButtonElement>('th button.sort')].find((b) =>
      b.textContent!.includes('כותרת'),
    )!;

    titleHeader.click(); // sort by title, descending: request goes out...
    await render();
    const [byTitleDesc] = searches();
    titleHeader.click(); // ...and the user clicks again before it answered
    await render();
    const [byTitleAsc] = searches();

    expect(byTitleDesc.request.params.get('sortDirection')).toBe('Desc');
    expect(byTitleDesc.cancelled).toBe(true); // the outdated request was aborted
    expect(byTitleAsc.request.params.get('sortDirection')).toBe('Asc');
    expect(byTitleAsc.cancelled).toBe(false);
  });

  it('on 409 shows a warning with the current status and reloads the list', async () => {
    await openWithOneRow();
    const select = page.querySelector<HTMLSelectElement>('app-requests-table tbody tr select')!;

    select.value = 'InProgress';
    select.dispatchEvent(new Event('change'));
    await render();
    const update: TestRequest = http.expectOne('/api/requests/1/status');
    expect(update.request.body).toEqual({ status: 'InProgress', rowVersion: row.rowVersion, changedBy: 'demo-user' });

    update.flush(
      { title: 'Concurrency conflict', status: 409, current: { ...row, status: 'Waiting', rowVersion: 'AAAAAAAAB9I=' } },
      { status: 409, statusText: 'Conflict' },
    );
    await render();

    const notice = page.querySelector('.notice')!;
    expect(notice.className).toContain('warning');
    expect(notice.textContent).toContain('משתמש אחר');
    expect(notice.textContent).toContain('ממתינה'); // the status someone else saved
    expect(searches()).toHaveLength(1); // the list is re-read so the table shows the real state
    http.expectOne('/api/requests/summary');
  });

  it('shows the empty state when nothing matches', async () => {
    fixture = TestBed.createComponent(RequestsPageComponent);
    page = fixture.nativeElement;
    await render();
    http.expectOne('/api/requests/summary').flush(summary);
    searches()[0].flush(onePage([]));
    await render();

    expect(page.querySelector('.state.empty')?.textContent).toContain('לא נמצאו פניות');
  });

  it('shows the error state with a retry button when the server fails, and retry reloads', async () => {
    fixture = TestBed.createComponent(RequestsPageComponent);
    page = fixture.nativeElement;
    await render();
    http.expectOne('/api/requests/summary').flush(summary);
    searches()[0].flush({ title: 'An unexpected error occurred.' }, { status: 500, statusText: 'Server Error' });
    await render();

    const error = page.querySelector('.state.error')!;
    expect(error.textContent).toContain('An unexpected error occurred.');

    error.querySelector('button')!.click();
    await render();
    expect(searches()).toHaveLength(1);
  });
});
