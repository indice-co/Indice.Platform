import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideArchive, LucideCalendarClock, LucideInbox, LucideOctagonAlert, LucideRotateCw } from '@lucide/angular';

import { QueueMessageStatus } from '../../core/services/workers-api.service';

/** The icon of a message status, the same on the queue list and the queue page. Sized and coloured by the host. */
@Component({
  selector: 'app-queue-status-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideArchive, LucideCalendarClock, LucideInbox, LucideOctagonAlert, LucideRotateCw],
  host: { class: 'inline-flex shrink-0', 'aria-hidden': 'true' },
  template: `
    @switch (status()) {
      @case ('Scheduled') {
        <svg lucideCalendarClock class="size-full"></svg>
      }
      @case ('Enqueued') {
        <svg lucideInbox class="size-full"></svg>
      }
      @case ('Retrying') {
        <svg lucideRotateCw class="size-full"></svg>
      }
      @case ('Poison') {
        <svg lucideOctagonAlert class="size-full"></svg>
      }
      @default {
        <svg lucideArchive class="size-full"></svg>
      }
    }
  `,
})
export class QueueStatusIconComponent {
  readonly status = input.required<QueueMessageStatus>();
}
