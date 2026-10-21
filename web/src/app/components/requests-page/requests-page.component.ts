import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Subject, catchError, combineLatest, map, of, scan, startWith, switchMap } from 'rxjs';
import { describeError } from '../../helpers/describe-error';
import type { RequestSortField } from '../../enums/request-sort-field';
import type { RequestStatus } from '../../enums/request-status';
import type { RequestFilters, RequestQueryDto } from '../../models/request-query.dto';
import type { RequestDto } from '../../models/request.dto';
import { RequestsApiService } from '../../services/requests-api.service';
import { BulkActionBarComponent } from '../bulk-action-bar/bulk-action-bar.component';
import { HistoryDialogComponent } from '../history-dialog/history-dialog.component';
import {
  INITIAL_LIST_STATE,
  INITIAL_QUERY,
  ListEvent,
  reduceListState,
} from '../../helpers/list-state';
import { PagerComponent } from '../pager/pager.component';
import { RequestFiltersComponent } from '../request-filters/request-filters.component';
import { RequestsTableComponent } from '../requests-table/requests-table.component';
import {
  Notice,
  bulkResultNotice,
  errorNotice,
  statusUpdateFailedNotice,
  statusUpdatedNotice,
} from '../../helpers/status-messages';
import { SummaryPanelComponent } from '../summary-panel/summary-panel.component';

/**
 * Container: owns the query state and talks to the API service.
 * Child components are presentational (inputs/outputs only), except the history dialog which loads its own data.
 */
@Component({
  selector: 'app-requests-page',
  imports: [
    FormsModule,
    RequestFiltersComponent,
    RequestsTableComponent,
    PagerComponent,
    SummaryPanelComponent,
    BulkActionBarComponent,
    HistoryDialogComponent,
  ],
  templateUrl: './requests-page.component.html',
  styleUrl: './requests-page.component.scss',
})
export class RequestsPageComponent {
  private readonly api = inject(RequestsApiService);

  protected readonly query = signal<RequestQueryDto>(INITIAL_QUERY);
  protected readonly currentUser = signal('demo-user');
  protected readonly notice = signal<Notice | null>(null);
  protected readonly pendingId = signal<number | null>(null);
  protected readonly selectedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly bulkRunning = signal(false);
  protected readonly historyFor = signal<RequestDto | null>(null);

  /** Emits after any write, so the list and the summary are re-read from the server. */
  private readonly reload$ = new Subject<void>();

  // switchMap: when the query changes while a search is in flight, the old HTTP call is unsubscribed,
  // which aborts it in the browser and cancels it on the server (CancellationToken).
  protected readonly list = toSignal(
    combineLatest([toObservable(this.query), this.reload$.pipe(startWith(undefined))]).pipe(
      switchMap(([query]) =>
        this.api.search(query).pipe(
          map((data): ListEvent => ({ kind: 'loaded', data })),
          catchError((error) => of<ListEvent>({ kind: 'error', message: describeError(error) })),
          startWith<ListEvent>({ kind: 'loading' }),
        ),
      ),
      scan(reduceListState, INITIAL_LIST_STATE),
    ),
    // toObservable emits its first value asynchronously, so the signal starts in the loading state.
    { initialValue: INITIAL_LIST_STATE },
  );

  protected readonly summary = toSignal(
    this.reload$.pipe(
      startWith(undefined),
      switchMap(() => this.api.summary().pipe(catchError(() => of(null)))),
    ),
    { initialValue: null },
  );

  protected readonly rows = computed(() => this.list().data?.items ?? []);
  protected readonly selectedRows = computed(() =>
    this.rows().filter((r) => this.selectedIds().has(r.id)),
  );

  protected onFiltersChange(filters: RequestFilters): void {
    this.updateQuery({ ...filters, page: 1 });
  }

  protected onSortChange(field: RequestSortField): void {
    const { sortBy, sortDirection } = this.query();
    const direction = sortBy === field && sortDirection === 'Desc' ? 'Asc' : 'Desc';
    this.updateQuery({ sortBy: field, sortDirection: direction, page: 1 });
  }

  protected onPageChange(page: number): void {
    this.updateQuery({ page });
  }

  protected onPageSizeChange(pageSize: number): void {
    this.updateQuery({ pageSize, page: 1 });
  }

  protected toggleSelected(id: number): void {
    const next = new Set(this.selectedIds());
    if (!next.delete(id)) next.add(id);
    this.selectedIds.set(next);
  }

  protected toggleAll(selectAll: boolean): void {
    this.selectedIds.set(selectAll ? new Set(this.rows().map((r) => r.id)) : new Set());
  }

  protected changeStatus({
    request,
    status,
  }: {
    request: RequestDto;
    status: RequestStatus;
  }): void {
    this.pendingId.set(request.id);
    this.api.updateStatus(request, status, this.currentUser()).subscribe({
      next: (updated) => this.afterWrite(statusUpdatedNotice(updated)),
      // Reload on failure too: after a 409 the table must show the real current state.
      error: (error: unknown) => this.afterWrite(statusUpdateFailedNotice(request.id, error)),
    });
  }

  protected runBulk(status: RequestStatus): void {
    const rows = this.selectedRows();
    if (rows.length === 0) return;

    this.bulkRunning.set(true);
    this.api.bulkUpdateStatus(rows, status, this.currentUser()).subscribe({
      next: (result) => {
        this.selectedIds.set(new Set());
        this.afterWrite(bulkResultNotice(result));
      },
      error: (error: unknown) => {
        this.bulkRunning.set(false);
        this.notice.set(errorNotice(error));
      },
    });
  }

  protected retry(): void {
    this.reload$.next();
  }

  private afterWrite(notice: Notice): void {
    this.pendingId.set(null);
    this.bulkRunning.set(false);
    this.notice.set(notice);
    this.reload$.next();
  }

  private updateQuery(patch: Partial<RequestQueryDto>): void {
    this.query.update((q) => ({ ...q, ...patch }));
    this.selectedIds.set(new Set()); // selection is per visible page
  }
}
