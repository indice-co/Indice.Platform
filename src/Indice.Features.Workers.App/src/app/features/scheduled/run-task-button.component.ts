import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { LucidePlay } from '@lucide/angular';
import { finalize } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ToastService } from '../../core/services/toast.service';
import { ScheduledTaskInfo, WorkersApiService } from '../../core/services/workers-api.service';

/**
 * "Run now" of a scheduled task, shared by the list and the detail page. The API only records the
 * request; a worker that hosts the task picks it up on its next poll, so until then the button
 * reads "Run requested". A task without a row (never run) can't be requested — no worker has
 * claimed it yet.
 */
@Component({
  selector: 'app-run-task-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LucidePlay],
  template: `
    @if (requestedAt(); as at) {
      <span
        class="badge badge-info badge-soft whitespace-nowrap"
        [class.badge-sm]="size() === 'xs'"
        [title]="'Requested ' + (at | date: 'medium') + ' — waiting for a worker to pick it up'"
      >
        <span class="loading loading-dots loading-xs" aria-hidden="true"></span>
        Run requested
      </span>
    } @else {
      <button
        type="button"
        class="btn btn-soft btn-primary gap-1.5 whitespace-nowrap"
        [class.btn-xs]="size() === 'xs'"
        [class.btn-sm]="size() === 'sm'"
        [disabled]="!task().hasRun || busy()"
        [title]="title()"
        (click)="$event.stopPropagation(); run()"
      >
        @if (busy()) {
          <span class="loading loading-spinner loading-xs" aria-hidden="true"></span>
        } @else {
          <svg lucidePlay [class]="size() === 'xs' ? 'size-3.5' : 'size-4'"></svg>
        }
        Run now
      </button>
    }
  `,
})
export class RunTaskButtonComponent {
  readonly task = input.required<ScheduledTaskInfo>();
  readonly size = input<'xs' | 'sm'>('sm');

  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly busy = signal(false);
  /** Follows the server; set at once on success, so the button doesn't flash back until the next poll. */
  protected readonly requestedAt = linkedSignal(() => this.task().runRequestedAt ?? null);

  protected readonly title = computed(() => {
    const task = this.task();
    if (!task.hasRun) {
      return 'Available after the first run';
    }
    return task.enabled
      ? 'Run once now, outside the schedule'
      : 'Run once now — the task stays disabled for its schedule';
  });

  protected run(): void {
    this.busy.set(true);
    this.api
      .runScheduledTask(this.task().id)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.requestedAt.set(result.requestedAt);
          this.toasts.show('success', `Run of ${this.task().name} requested.`);
          this.live.refreshNow();
        },
        error: (e: unknown) =>
          this.toasts.show(
            'error',
            e instanceof HttpErrorResponse && e.status === 404
              ? 'The task has no record yet — it can run on demand after its first run.'
              : describeError(e),
          ),
      });
  }
}
