import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { STATUS_LABELS } from '../../constants/labels';
import { RequestStatus, STATUSES } from '../../enums/request-status';

/** Presentational: shown while rows are selected; reports which status to apply. */
@Component({
  selector: 'app-bulk-action-bar',
  imports: [FormsModule],
  templateUrl: './bulk-action-bar.component.html',
  styleUrl: './bulk-action-bar.component.scss',
})
export class BulkActionBarComponent {
  readonly selectedCount = input.required<number>();
  readonly running = input(false);

  readonly apply = output<RequestStatus>();
  readonly clear = output<void>();

  protected readonly statuses = STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly targetStatus = signal<RequestStatus>('InProgress');
}
