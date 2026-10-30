import { Component, input } from '@angular/core';

/**
 * Page title row. Project actions with `<div actions>…</div>`, a count beneath the title with `<app-list-count>` and an
 * optional leading element (e.g. back link) with `[leading]`. Breadcrumbs are rendered by the shell.
 */
@Component({
  selector: 'app-page-header',
  host: { class: 'mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between' },
  template: `
    <div class="min-w-0">
      <ng-content select="[leading]" />
      <h1 class="text-2xl font-bold tracking-tight text-balance sm:text-[1.75rem]">{{ title() }}</h1>
      <ng-content select="app-list-count" />
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
