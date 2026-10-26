import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  linkedSignal,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ToastService } from '../../core/services/toast.service';
import { ScheduledTaskInfo, WorkersApiService } from '../../core/services/workers-api.service';

/**
 * The Enabled switch of a scheduled task, shared by the list and the detail page. Optimistic: it
 * flips at once, reverts if the request fails, and stays disabled while the request is in flight.
 * A task without a row (never run) can't be toggled — the API has nothing to update.
 */
@Component({
  selector: 'app-task-enabled-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-flex align-middle"
      [class.tooltip]="!task().hasRun"
      [class.tooltip-left]="!task().hasRun"
      [attr.data-tip]="task().hasRun ? null : 'Available after the first run'"
    >
      <input
        type="checkbox"
        class="toggle toggle-sm toggle-success"
        [checked]="checked()"
        [disabled]="!task().hasRun || busy()"
        [attr.aria-label]="(checked() ? 'Disable ' : 'Enable ') + task().name"
        (click)="$event.stopPropagation()"
        (change)="onChange($event)"
      />
    </span>
  `,
})
export class TaskEnabledToggleComponent {
  readonly task = input.required<ScheduledTaskInfo>();

  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly busy = signal(false);

  /**
   * Follows the server value on every poll, except while a request is in flight: a poll that
   * started before the PUT would otherwise flip the switch back for a moment.
   */
  protected readonly checked = linkedSignal<ScheduledTaskInfo, boolean>({
    source: this.task,
    computation: (task, previous) =>
      previous && untracked(this.busy) ? previous.value : task.enabled,
  });

  protected onChange(event: Event): void {
    const box = event.target as HTMLInputElement;
    const enabled = box.checked;
    this.checked.set(enabled);
    this.busy.set(true);
    this.api
      .setScheduledTaskEnabled(this.task().id, enabled)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.toasts.show('success', enabled ? 'Task enabled' : 'Task disabled');
          // Cancels any poll still in flight, so the next data already carries the new value.
          this.live.refreshNow();
        },
        error: (error: unknown) => {
          this.checked.set(!enabled);
          // A synchronous failure leaves the bound value unchanged (true → false → true within
          // one turn), so change detection would not touch the box the user just clicked.
          box.checked = !enabled;
          this.busy.set(false);
          this.toasts.show('error', describeError(error));
        },
      });
  }
}
