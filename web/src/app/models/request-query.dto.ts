import type { Priority } from '../enums/priority';
import type { RequestSortField } from '../enums/request-sort-field';
import type { RequestStatus } from '../enums/request-status';
import type { SortDirection } from '../enums/sort-direction';

/** The part of the query that comes from the filter form. Empty string means "no filter". */
export interface RequestFilters {
  search: string;
  status: RequestStatus | '';
  priority: Priority | '';
  organizationName: string;
  assignedTo: string;
  createdFrom: string; // yyyy-MM-dd from <input type="date">
  createdTo: string;
}

/** Mirrors RequestQueryDto (sent as query-string parameters of GET /api/requests). */
export interface RequestQueryDto extends RequestFilters {
  page: number;
  pageSize: number;
  sortBy: RequestSortField;
  sortDirection: SortDirection;
}
