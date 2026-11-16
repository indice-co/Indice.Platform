import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideInbox } from '@lucide/angular';

/**
 * Centered, muted "nothing here" block. Project an `[icon]` element to replace the default inbox
 * icon, and any other content (a link or button) as the action underneath.
 */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideInbox],
  template: `
    <div class="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
      <div
        class="mb-1 grid size-14 place-items-center rounded-full bg-base-content/6 text-base-content/45 [&>svg]:size-7"
        aria-hidden="true"
      >
        <ng-content select="[icon]">
          <svg lucideInbox></svg>
        </ng-content>
      </div>
      <p class="font-medium text-base-content/80">{{ title() }}</p>
      @if (text()) {
        <p class="max-w-md text-sm text-base-content/70">{{ text() }}</p>
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
