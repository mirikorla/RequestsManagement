import { DatePipe } from '@angular/common';
import {
  Component,
  ElementRef,
  afterNextRender,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { STATUS_LABELS } from '../../constants/labels';
import type { RequestDto } from '../../models/request.dto';
import type { StatusHistoryDto } from '../../models/status-history.dto';
import { describeError } from '../../helpers/describe-error';
import { RequestsApiService } from '../../services/requests-api.service';

type HistoryState =
  | { kind: 'loading' }
  | { kind: 'loaded'; items: StatusHistoryDto[] }
  | { kind: 'error'; message: string };

/** Modal with the audit trail of one request. Loads its own data because it is opened on demand. */
@Component({
  selector: 'app-history-dialog',
  imports: [DatePipe],
  templateUrl: './history-dialog.component.html',
  styleUrl: './history-dialog.component.scss',
})
export class HistoryDialogComponent {
  private readonly api = inject(RequestsApiService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly request = input.required<RequestDto>();
  readonly closed = output<void>();

  protected readonly labels = STATUS_LABELS;

  protected readonly state = toSignal(
    toObservable(this.request).pipe(
      switchMap((r) =>
        this.api.history(r.id).pipe(
          map((items): HistoryState => ({ kind: 'loaded', items })),
          catchError((error) => of<HistoryState>({ kind: 'error', message: describeError(error) })),
          startWith<HistoryState>({ kind: 'loading' }),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as HistoryState },
  );

  constructor() {
    afterNextRender(() => this.dialog().nativeElement.showModal());
  }
}
