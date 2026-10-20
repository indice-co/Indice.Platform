import { Component, input } from '@angular/core';

/**
 * Title row of a page. Project buttons with the `actions` attribute; anything else renders under the title.
 *
 * ```html
 * <app-page-header [title]="'cases.title' | translate" [subtitle]="count + ' results'">
 *   <button actions class="btn btn-primary">…</button>
 * </app-page-header>
 * ```
 */
@Component({
  selector: 'app-page-header',
  template: `
    <div class="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
      <div class="min-w-0">
        <ng-content select="[eyebrow]" />
        <h1 class="text-[1.375rem] leading-[1.75rem] font-semibold tracking-[-0.01em]">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="mt-1 text-[0.9375rem] text-quiet">{{ subtitle() }}</p>
        }
        <ng-content />
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <ng-content select="[actions]" />
      </div>
    </div>
  `,
  host: { class: 'mb-6 block pt-4' }
})
export class PageHeaderComponent {
  public readonly title = input<string | null | undefined>('');
  public readonly subtitle = input<string | null | undefined>();
}
