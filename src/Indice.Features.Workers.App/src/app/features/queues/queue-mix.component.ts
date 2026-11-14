import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { QueueInfo } from '../../core/services/workers-api.service';
import { LIFECYCLE, STATUS_TONE, countOf } from './queue-status';

/**
 * A queue's messages as one bar split by status, in lifecycle order, so a glance tells a healthy queue (all blue)
 * from one filling up with retries or poison. Renders nothing for an empty queue.
 */
@Component({
  selector: 'app-queue-mix',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    @if (segments().length) {
      <div
        class="flex gap-px overflow-hidden rounded-full bg-base-300"
        [class]="thin() ? 'h-1.5' : 'h-2'"
        role="img"
        [attr.aria-label]="label()"
        [title]="label()"
      >
        @for (segment of segments(); track segment.status) {
          <span [class]="segment.bar" [style.width.%]="segment.percent"></span>
        }
      </div>
    }
  `,
})
export class QueueMixComponent {
  readonly queue = input.required<QueueInfo | null>();
  readonly thin = input(false);

  protected readonly segments = computed(() => {
    const queue = this.queue();
    const counts = LIFECYCLE.map((status) => ({ status, count: countOf(queue, status) })).filter((s) => s.count > 0);
    const total = counts.reduce((sum, s) => sum + s.count, 0);
    return total
      ? counts.map((s) => ({ ...s, bar: STATUS_TONE[s.status].bar, percent: (s.count / total) * 100 }))
      : [];
  });

  protected readonly label = computed(() =>
    this.segments()
      .map((s) => `${s.count} ${s.status.toLowerCase()}`)
      .join(', '),
  );
}
