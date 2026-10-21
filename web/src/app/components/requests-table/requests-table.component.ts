import { DatePipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { PRIORITY_LABELS, STATUS_LABELS } from '../../constants/labels';
import { ALLOWED_TRANSITIONS } from '../../constants/status-transitions';
import type { RequestSortField } from '../../enums/request-sort-field';
import type { RequestStatus } from '../../enums/request-status';
import type { SortDirection } from '../../enums/sort-direction';
import type { RequestDto } from '../../models/request.dto';

/** Presentational: renders one page of rows and reports user intent. Holds no data of its own. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe],
  templateUrl: './requests-table.component.html',
  styleUrl: './requests-table.component.scss',
})
export class RequestsTableComponent {
  readonly rows = input.required<RequestDto[]>();
  readonly sortBy = input.required<RequestSortField>();
  readonly sortDirection = input.required<SortDirection>();
  readonly pendingId = input<number | null>(null);
  readonly selectedIds = input<ReadonlySet<number>>(new Set());

  readonly sortChange = output<RequestSortField>();
  readonly statusChange = output<{ request: RequestDto; status: RequestStatus }>();
  readonly toggleSelect = output<number>();
  readonly toggleAll = output<boolean>();
  readonly openHistory = output<RequestDto>();

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;
  protected readonly transitions = ALLOWED_TRANSITIONS;

  protected readonly allSelected = computed(
    () => this.rows().length > 0 && this.rows().every((r) => this.selectedIds().has(r.id)),
  );

  protected readonly columns: { field: RequestSortField | null; label: string }[] = [
    { field: null, label: '#' },
    { field: 'Title', label: 'כותרת' },
    { field: 'OrganizationName', label: 'ארגון' },
    { field: 'Status', label: 'סטטוס' },
    { field: 'Priority', label: 'עדיפות' },
    { field: null, label: 'מטפל' },
    { field: 'CreatedAt', label: 'נוצרה' },
    { field: 'UpdatedAt', label: 'עודכנה' },
  ];

  protected sortIndicator(field: RequestSortField | null): string {
    if (!field || field !== this.sortBy()) return '';
    return this.sortDirection() === 'Asc' ? '▲' : '▼';
  }

  protected ariaSort(field: RequestSortField | null): string | null {
    if (!field) return null;
    if (field !== this.sortBy()) return 'none';
    return this.sortDirection() === 'Asc' ? 'ascending' : 'descending';
  }

  protected onStatusSelect(request: RequestDto, select: HTMLSelectElement): void {
    const status = select.value as RequestStatus;
    select.value = ''; // the row re-renders with the server's answer, not with the optimistic choice
    if (status) this.statusChange.emit({ request, status });
  }
}
