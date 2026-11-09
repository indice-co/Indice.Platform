import { DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/** "42 campaigns" line shown beneath a grid's title. A `null` count (not loaded yet) renders a placeholder. */
@Component({
  selector: 'app-list-count',
  imports: [DecimalPipe, TranslatePipe],
  host: { class: 'block text-sm text-base-content/60', 'aria-live': 'polite' },
  template: `
    @let value = count();
    @if (value === null) {
      <span class="skeleton inline-block h-4 w-24 align-middle"></span>
    } @else {
      <span class="font-semibold text-base-content tabular">{{ value | number }}</span> {{ (value === 1 ? one() : other()) | translate }}
    }
  `
})
export class ListCountComponent {
  public readonly count = input.required<number | null>();
  /** Translation key of the singular entity name. */
  public readonly one = input.required<string>();
  /** Translation key of the plural entity name. */
  public readonly other = input.required<string>();
}
