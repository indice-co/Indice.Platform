import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';

/**
 * Page title row. Project actions with `<div actions>…</div>`; any other content renders beneath the title.
 * Set `count` to show the total of a list next to the title.
 */
@Component({
  selector: 'app-page-header',
  imports: [DecimalPipe],
  host: { class: 'mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between' },
  template: `
    <div class="min-w-0">
      <h1 class="flex flex-wrap items-center gap-x-3 gap-y-1 text-2xl font-semibold tracking-tight text-balance sm:text-[1.75rem]">
        <span>{{ title() }}</span>
        @if (count() != null) {
          <span class="badge badge-soft badge-primary tabular font-semibold tracking-normal">{{ count() | number }}</span>
        }
      </h1>
      @if (subtitle()) {
        <p class="mt-1 max-w-prose text-sm text-base-content/70">{{ subtitle() }}</p>
      }
      <ng-content />
    </div>
    <div class="flex flex-wrap items-center gap-2">
      <ng-content select="[actions]" />
    </div>
  `
})
export class PageHeaderComponent {
  public readonly title = input.required<string>();
  public readonly subtitle = input<string>();
  /** Total number of items of the listed entity; hidden while `null` or `undefined`. */
  public readonly count = input<number | null | undefined>();
}
