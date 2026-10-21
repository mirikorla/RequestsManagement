/** Mirrors PagedResultDto<T>: one page of results plus the totals the pager needs. */
export interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
