import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { PRIORITY_LABELS, STATUS_LABELS } from '../../constants/labels';
import { PRIORITIES } from '../../enums/priority';
import { STATUSES } from '../../enums/request-status';
import type { RequestsSummaryDto } from '../../models/requests-summary.dto';

/** Aggregations from GET /api/requests/summary (computed by GROUP BY on the server, cached there). */
@Component({
  selector: 'app-summary-panel',
  imports: [DecimalPipe, DatePipe],
  templateUrl: './summary-panel.component.html',
  styleUrl: './summary-panel.component.scss',
})
export class SummaryPanelComponent {
  readonly summary = input<RequestsSummaryDto | null>(null);

  protected readonly statuses = STATUSES;
  protected readonly priorities = PRIORITIES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;
}
