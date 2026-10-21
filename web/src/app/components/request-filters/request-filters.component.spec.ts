import { TestBed } from '@angular/core/testing';
import type { RequestFilters } from '../../models/request-query.dto';
import { RequestFiltersComponent } from './request-filters.component';

describe('RequestFiltersComponent', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  function setup() {
    const fixture = TestBed.createComponent(RequestFiltersComponent);
    fixture.detectChanges();
    const emitted: RequestFilters[] = [];
    fixture.componentInstance.filtersChange.subscribe((f) => emitted.push(f));
    const search = fixture.nativeElement.querySelector('input[formcontrolname="search"]') as HTMLInputElement;
    const type = (text: string) => {
      search.value = text;
      search.dispatchEvent(new Event('input'));
    };
    return { emitted, type };
  }

  it('debounces typing: six keystrokes send ONE filter change, 350 ms after the last one', () => {
    const { emitted, type } = setup();

    for (const text of ['R', 'Re', 'Ref', 'Refu', 'Refun', 'Refund']) {
      type(text);
      vi.advanceTimersByTime(100); // typing faster than the debounce window
    }
    expect(emitted).toHaveLength(0); // still typing

    vi.advanceTimersByTime(350);

    expect(emitted).toHaveLength(1);
    expect(emitted[0].search).toBe('Refund');
  });

  it('does not emit again when the text ends up the same as before', () => {
    const { emitted, type } = setup();
    type('Refund');
    vi.advanceTimersByTime(350);

    type('Refun');
    type('Refund'); // back to the same value within the window
    vi.advanceTimersByTime(350);

    expect(emitted).toHaveLength(1);
  });
});
