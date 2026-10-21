import type { PagedResultDto } from '../models/paged-result.dto';
import type { RequestDto } from '../models/request.dto';
import { INITIAL_LIST_STATE, reduceListState } from './list-state';

const page: PagedResultDto<RequestDto> = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

describe('reduceListState', () => {
  it('keeps the previous page while the next one loads, so the table does not flicker', () => {
    const loaded = reduceListState(INITIAL_LIST_STATE, { kind: 'loaded', data: page });

    const loadingAgain = reduceListState(loaded, { kind: 'loading' });

    expect(loadingAgain).toEqual({ loading: true, data: page, error: null });
  });

  it('shows the error and drops stale data when a request fails', () => {
    const loaded = reduceListState(INITIAL_LIST_STATE, { kind: 'loaded', data: page });

    const failed = reduceListState(loaded, { kind: 'error', message: 'boom' });

    expect(failed).toEqual({ loading: false, data: null, error: 'boom' });
  });

  it('clears a previous error when a new request starts', () => {
    const failed = reduceListState(INITIAL_LIST_STATE, { kind: 'error', message: 'boom' });

    expect(reduceListState(failed, { kind: 'loading' }).error).toBeNull();
  });
});
