import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Page title + optional subtitle; projected content sits on the right (actions, filters). */
@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="flex flex-wrap items-end justify-between gap-3 pb-4">
      <div class="min-w-0">
        <h1 class="text-2xl font-semibold tracking-tight text-base-content">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="mt-0.5 text-sm text-base-content/60">{{ subtitle() }}</p>
        }
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <ng-content />
      </div>
    </header>
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null | undefined>(null);
}
