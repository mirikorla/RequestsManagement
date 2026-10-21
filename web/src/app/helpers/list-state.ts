import type { PagedResultDto } from '../models/paged-result.dto';
import type { RequestQueryDto } from '../models/request-query.dto';
import type { RequestDto } from '../models/request.dto';

/** What the list area shows. The previous page is kept while the next one loads, so the table doesn't flicker. */
export interface ListState {
  loading: boolean;
  data: PagedResultDto<RequestDto> | null;
  error: string | null;
}

export type ListEvent =
  | { kind: 'loading' }
  | { kind: 'loaded'; data: PagedResultDto<RequestDto> }
  | { kind: 'error'; message: string };

export const INITIAL_LIST_STATE: ListState = { loading: true, data: null, error: null };

export function reduceListState(state: ListState, event: ListEvent): ListState {
  switch (event.kind) {
    case 'loading':
      return { ...state, loading: true, error: null };
    case 'loaded':
      return { loading: false, data: event.data, error: null };
    case 'error':
      return { loading: false, data: null, error: event.message };
  }
}

export const INITIAL_QUERY: RequestQueryDto = {
  search: '',
  status: '',
  priority: '',
  organizationName: '',
  assignedTo: '',
  createdFrom: '',
  createdTo: '',
  page: 1,
  pageSize: 20,
  sortBy: 'CreatedAt',
  sortDirection: 'Desc',
};
