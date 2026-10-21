import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { RequestStatus } from '../enums/request-status';
import type { BulkUpdateStatusResultDto } from '../models/bulk/bulk-update-status-result.dto';
import type { BulkUpdateStatusDto } from '../models/bulk/bulk-update-status.dto';
import type { PagedResultDto } from '../models/paged-result.dto';
import type { RequestQueryDto } from '../models/request-query.dto';
import type { RequestDto } from '../models/request.dto';
import type { RequestsSummaryDto } from '../models/requests-summary.dto';
import type { StatusHistoryDto } from '../models/status-history.dto';
import type { UpdateStatusDto } from '../models/update-status.dto';

/** The only place that knows URLs and query-string shape. Components never call HttpClient directly. */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/requests';

  search(query: RequestQueryDto): Observable<PagedResultDto<RequestDto>> {
    return this.http.get<PagedResultDto<RequestDto>>(this.baseUrl, { params: toParams(query) });
  }

  summary(): Observable<RequestsSummaryDto> {
    return this.http.get<RequestsSummaryDto>(`${this.baseUrl}/summary`);
  }

  history(id: number): Observable<StatusHistoryDto[]> {
    return this.http.get<StatusHistoryDto[]>(`${this.baseUrl}/${id}/history`);
  }

  updateStatus(
    request: RequestDto,
    status: RequestStatus,
    changedBy: string,
  ): Observable<RequestDto> {
    const body: UpdateStatusDto = { status, rowVersion: request.rowVersion, changedBy };
    return this.http.patch<RequestDto>(`${this.baseUrl}/${request.id}/status`, body);
  }

  bulkUpdateStatus(
    requests: RequestDto[],
    status: RequestStatus,
    changedBy: string,
  ): Observable<BulkUpdateStatusResultDto> {
    const body: BulkUpdateStatusDto = {
      status,
      changedBy,
      items: requests.map((r) => ({ id: r.id, rowVersion: r.rowVersion })),
    };
    return this.http.post<BulkUpdateStatusResultDto>(`${this.baseUrl}/status/bulk`, body);
  }
}

function toParams(q: RequestQueryDto): HttpParams {
  let params = new HttpParams()
    .set('page', q.page)
    .set('pageSize', q.pageSize)
    .set('sortBy', q.sortBy)
    .set('sortDirection', q.sortDirection);

  const optional: Record<string, string> = {
    search: q.search.trim(),
    status: q.status,
    priority: q.priority,
    organizationName: q.organizationName.trim(),
    assignedTo: q.assignedTo.trim(),
    createdFrom: q.createdFrom,
    // The date picker gives a day; include that whole day.
    createdTo: q.createdTo ? `${q.createdTo}T23:59:59.999` : '',
  };
  for (const [key, value] of Object.entries(optional)) {
    if (value) params = params.set(key, value);
  }
  return params;
}
