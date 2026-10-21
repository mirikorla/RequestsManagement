import { DecimalPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-pager',
  imports: [DecimalPipe],
  templateUrl: './pager.component.html',
  styleUrl: './pager.component.scss',
})
export class PagerComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input.required<number>();

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  // The server caps PageSize at 100 (RequestQueryDto.MaxPageSize).
  protected readonly sizes = [10, 20, 50, 100];
}
