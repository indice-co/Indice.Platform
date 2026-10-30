import { Component, input } from '@angular/core';

/** Page title row. Project actions with `<div actions>…</div>`; any other content renders beneath the title. */
@Component({
  selector: 'app-page-header',
  host: { class: 'mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between' },
  template: `
    <div class="min-w-0">
      <h1 class="text-2xl font-semibold tracking-tight text-balance sm:text-[1.75rem]">{{ title() }}</h1>
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
}
