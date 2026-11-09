import { Component, input } from '@angular/core';

/**
 * Empty / error placeholder for lists and panels. Project an icon with `[icon]` and actions with `[actions]`.
 */
@Component({
  selector: 'app-empty-state',
  host: { class: 'flex flex-col items-center justify-center gap-3 px-6 py-14 text-center' },
  template: `
    <div class="grid size-12 place-items-center rounded-full bg-base-200 text-base-content/60">
      <ng-content select="[icon]" />
    </div>
    <div class="max-w-sm space-y-1">
      <p class="font-semibold">{{ title() }}</p>
      @if (message()) {
        <p class="text-sm text-base-content/70">{{ message() }}</p>
      }
    </div>
    <ng-content select="[actions]" />
  `
})
export class EmptyStateComponent {
  public readonly title = input.required<string>();
  public readonly message = input<string>();
}
