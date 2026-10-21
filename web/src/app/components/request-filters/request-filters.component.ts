import { Component, inject, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { PRIORITY_LABELS, STATUS_LABELS } from '../../constants/labels';
import { PRIORITIES } from '../../enums/priority';
import { STATUSES } from '../../enums/request-status';
import type { RequestFilters } from '../../models/request-query.dto';

/**
 * Filter form. Emits the whole filter set, debounced, and only when something actually changed,
 * so typing "invoice" sends one request instead of seven.
 */
@Component({
  selector: 'app-request-filters',
  imports: [ReactiveFormsModule],
  templateUrl: './request-filters.component.html',
  styleUrl: './request-filters.component.scss',
})
export class RequestFiltersComponent {
  protected readonly statuses = STATUSES;
  protected readonly priorities = PRIORITIES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;

  protected readonly form = inject(NonNullableFormBuilder).group({
    search: '',
    status: '' as RequestFilters['status'],
    priority: '' as RequestFilters['priority'],
    organizationName: '',
    assignedTo: '',
    createdFrom: '',
    createdTo: '',
  });

  readonly filtersChange = output<RequestFilters>();

  constructor() {
    this.form.valueChanges
      .pipe(
        debounceTime(350),
        map(() => this.form.getRawValue()),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        takeUntilDestroyed(),
      )
      .subscribe((filters) => this.filtersChange.emit(filters));
  }
}
