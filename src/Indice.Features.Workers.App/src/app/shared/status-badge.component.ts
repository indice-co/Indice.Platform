import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { QueueMessageStatus } from '../core/services/workers-api.service';

/** Every state the dashboard paints as a badge: message statuses, task states and last-run outcomes. */
export type BadgeStatus =
  | QueueMessageStatus
  | 'Idle'
  | 'Running'
  | 'Failed'
  | 'Ok'
  | 'Disabled'
  | 'NotRun';

/** Hangfire's status colours, expressed as daisyUI badge variants so they follow the theme. */
const STYLES: Record<BadgeStatus, { cls: string; label: string }> = {
  Enqueued: { cls: 'badge-info', label: 'Enqueued' },
  Scheduled: { cls: 'badge-secondary badge-soft', label: 'Scheduled' },
  Retrying: { cls: 'badge-warning', label: 'Retrying' },
  Poison: { cls: 'badge-error', label: 'Poison' },
  Dequeued: { cls: 'badge-ghost', label: 'Dequeued' },
  Idle: { cls: 'badge-ghost', label: 'Idle' },
  Running: { cls: 'badge-warning', label: 'Running' },
  Failed: { cls: 'badge-error', label: 'Failed' },
  Ok: { cls: 'badge-success badge-soft', label: 'Ok' },
  Disabled: { cls: 'badge-neutral badge-outline', label: 'Disabled' },
  NotRun: { cls: 'badge-ghost', label: 'Not run' },
};

/** A small status pill; `Running` adds a spinner so live work stands out while polling. */
@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="badge badge-sm whitespace-nowrap" [class]="style().cls">
      @if (status() === 'Running') {
        <span class="loading loading-spinner loading-xs" aria-hidden="true"></span>
      }
      {{ style().label }}
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly status = input.required<BadgeStatus>();

  protected readonly style = computed(() => STYLES[this.status()] ?? STYLES.Idle);
}
