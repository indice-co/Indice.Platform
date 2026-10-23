import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Centered, muted "nothing here" block. Project an `[icon]` element to replace the default inbox
 * icon, and any other content (a link or button) as the action underneath.
 */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
      <div class="text-base-content/30">
        <ng-content select="[icon]">
          <svg viewBox="0 0 24 24" fill="none" class="size-10" aria-hidden="true">
            <path
              d="M4 13l2.5-7.5A2 2 0 0 1 8.4 4h7.2a2 2 0 0 1 1.9 1.5L20 13M4 13v5a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-5M4 13h4l1.5 2h5L16 13h4"
              stroke="currentColor"
              stroke-width="1.6"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </ng-content>
      </div>
      <p class="font-medium text-base-content/80">{{ title() }}</p>
      @if (text()) {
        <p class="max-w-md text-sm text-base-content/55">{{ text() }}</p>
      }
      <div class="mt-2">
        <ng-content />
      </div>
    </div>
  `,
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly text = input<string | null>(null);
}
