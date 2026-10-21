import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { INITIAL_QUERY } from '../helpers/list-state';
import type { RequestDto } from '../models/request.dto';
import { RequestsApiService } from './requests-api.service';

describe('RequestsApiService', () => {
  let api: RequestsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(RequestsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify()); // no unexpected requests

  it('sends paging, sorting and only the filters that were set', () => {
    api.search({ ...INITIAL_QUERY, status: 'Waiting', search: '  Refund  ', createdTo: '2026-02-01' }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/requests');
    const params = req.request.params;
    expect(params.get('page')).toBe('1');
    expect(params.get('pageSize')).toBe('20');
    expect(params.get('sortBy')).toBe('CreatedAt');
    expect(params.get('status')).toBe('Waiting');
    expect(params.get('search')).toBe('Refund'); // trimmed
    expect(params.get('createdTo')).toBe('2026-02-01T23:59:59.999'); // the whole selected day
    expect(params.has('priority')).toBe(false); // empty filters are not sent
    expect(params.has('organizationName')).toBe(false);
    req.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  });

  it('sends the RowVersion the user saw, so the server can detect a concurrent change', () => {
    const request = { id: 5, rowVersion: 'AAAAAAAAB9E=' } as RequestDto;

    api.updateStatus(request, 'InProgress', 'dana').subscribe();

    const req = http.expectOne('/api/requests/5/status');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ status: 'InProgress', rowVersion: 'AAAAAAAAB9E=', changedBy: 'dana' });
    req.flush({});
  });

  it('sends every selected request with its own RowVersion in a bulk update', () => {
    const rows = [
      { id: 1, rowVersion: 'AAAAAAAAAAE=' },
      { id: 2, rowVersion: 'AAAAAAAAAAI=' },
    ] as RequestDto[];

    api.bulkUpdateStatus(rows, 'Completed', 'dana').subscribe();

    const req = http.expectOne('/api/requests/status/bulk');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      status: 'Completed',
      changedBy: 'dana',
      items: [
        { id: 1, rowVersion: 'AAAAAAAAAAE=' },
        { id: 2, rowVersion: 'AAAAAAAAAAI=' },
      ],
    });
    req.flush({ succeeded: 2, failed: 0, results: [] });
  });
});
