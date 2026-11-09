import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { EMPTY, Observable, catchError, combineLatest, finalize, map, of, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ToastService } from '../../core/services/toast.service';
import {
  BulkActionResult,
  QueueMessageDetails,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { JsonViewComponent } from '../../shared/json-view.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { describeBulkResult } from './queue-status';

/** How long the "Copied" confirmation stays on the id button. */
const COPIED_MS = 1500;

/**
 * One message (Hangfire's "Job details"): where it sits, when it becomes visible, how many attempts
 * it has had, and its payload. With the relational store a processed message is deleted, so "not
 * found" is the normal end of a message's life, not an error.
 */
@Component({
  selector: 'app-message-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    ConfirmDialogComponent,
    EmptyStateComponent,
    JsonViewComponent,
    RelativeTimePipe,
    StatusBadgeComponent,
  ],
  template: `
    <section class="space-y-4 p-4 sm:p-6">
      <div class="breadcrumbs text-sm">
        <ul>
          <li><a routerLink="/queues">Queues</a></li>
          @if (queueName(); as queue) {
            <li><a [routerLink]="['/queues', queue]">{{ queue }}</a></li>
          }
          <li>Message</li>
        </ul>
      </div>

      @if (error(); as text) {
        <div role="alert" class="alert alert-error alert-soft">{{ text }}</div>
      }

      @if (gone()) {
        <div class="card bg-base-100 shadow-sm">
          <app-empty-state
            title="This message is no longer in the queue"
            text="It was most likely processed — processed messages are removed from the store."
          >
            @if (queueName(); as queue) {
              <a [routerLink]="['/queues', queue]" class="btn btn-sm">Back to {{ queue }}</a>
            } @else {
              <a routerLink="/queues" class="btn btn-sm">Back to Queues</a>
            }
          </app-empty-state>
        </div>
      } @else if (message(); as m) {
        <header class="flex flex-wrap items-start justify-between gap-3">
          <div class="min-w-0 space-y-1">
            <div class="flex flex-wrap items-center gap-2">
              <h1 class="text-2xl font-semibold tracking-tight text-base-content">Message</h1>
              <app-status-badge [status]="m.status" />
            </div>
            <div class="flex items-center gap-1">
              <span class="break-all font-mono text-sm text-base-content/60">{{ m.id }}</span>
              <button
                type="button"
                class="btn btn-ghost btn-xs"
                (click)="copyId(m.id)"
                [attr.aria-label]="copied() ? 'Copied' : 'Copy message id'"
              >
                {{ copied() ? 'Copied' : 'Copy' }}
              </button>
            </div>
          </div>
          <div class="flex flex-wrap items-center gap-2">
            <button
              type="button"
              class="btn btn-primary btn-sm"
              [disabled]="busy()"
              (click)="requeue(m)"
            >
              {{ m.status === 'Scheduled' ? 'Enqueue now' : 'Requeue' }}
            </button>
            <button
              type="button"
              class="btn btn-error btn-outline btn-sm"
              [disabled]="busy()"
              (click)="deleteDialog.open()"
            >
              Delete
            </button>
          </div>
        </header>

        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <h2 class="card-title text-base">Overview</h2>
            <dl class="grid gap-x-6 gap-y-3 text-sm sm:grid-cols-[10rem_1fr]">
              <dt class="text-base-content/60">Queue</dt>
              <dd class="break-all font-mono">{{ m.queueName }}</dd>

              <dt class="text-base-content/60">Status</dt>
              <dd><app-status-badge [status]="m.status" /></dd>

              <dt class="text-base-content/60">Available at</dt>
              <dd>
                {{ m.date | date: 'yyyy-MM-dd HH:mm:ss' : 'UTC' }} UTC
                <span class="text-base-content/60">({{ m.date | relativeTime }})</span>
              </dd>

              <dt class="text-base-content/60">Attempts</dt>
              <dd class="tabular-nums">
                {{ m.dequeueCount }}{{ m.maxRetryCount != null ? ' of ' + m.maxRetryCount : '' }}
              </dd>
            </dl>
          </div>
        </div>

        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <h2 class="card-title text-base">Payload</h2>
            <app-json-view [json]="m.payload" />
          </div>
        </div>
      } @else if (!error()) {
        <div class="space-y-4">
          <div class="skeleton h-10 w-64"></div>
          <div class="skeleton h-40 w-full"></div>
          <div class="skeleton h-64 w-full"></div>
        </div>
      }
    </section>

    <app-confirm-dialog
      #deleteDialog
      title="Delete message?"
      message="Permanently delete this message. It will not be processed."
      confirmLabel="Delete"
      (confirmed)="delete()"
    />
  `,
})
export class MessageDetailPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Route param `:id`. */
  readonly id = input.required<string>();

  /** `null` until the first response, so the template can show skeletons instead of "gone". */
  protected readonly message = signal<QueueMessageDetails | null>(null);
  /** The message isn't in the store (any more). */
  protected readonly gone = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly copied = signal(false);

  /** Kept after the message disappears, so the breadcrumb and "Back" still lead to its queue. */
  private readonly lastQueue = signal<string | null>(null);
  protected readonly queueName = computed(
    () => this.message()?.logicalQueueName ?? this.lastQueue(),
  );

  private copiedTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.destroyRef.onDestroy(() => clearTimeout(this.copiedTimer));

    // Keeps polling after a 404: a failed attempt re-inserts the message with the same id, so a
    // message can come back as Retrying while someone is looking at it.
    combineLatest([toObservable(this.id), this.live.tick$])
      .pipe(
        switchMap(([id]) =>
          this.api.getMessage(id).pipe(
            map((message): QueueMessageDetails | null => message),
            catchError((e: unknown) => {
              if (e instanceof HttpErrorResponse && e.status === 404) {
                return of(null);
              }
              this.error.set(describeError(e));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((message) => {
        this.message.set(message);
        this.gone.set(message === null);
        if (message) {
          this.lastQueue.set(message.logicalQueueName);
        }
        this.error.set(null);
        this.live.markUpdated();
      });
  }

  protected copyId(id: string): void {
    navigator.clipboard?.writeText(id).then(() => {
      this.copied.set(true);
      clearTimeout(this.copiedTimer);
      this.copiedTimer = setTimeout(() => this.copied.set(false), COPIED_MS);
    });
  }

  protected requeue(message: QueueMessageDetails): void {
    const verb = message.status === 'Scheduled' ? 'enqueued' : 'requeued';
    this.run(this.api.requeueMessages([message.id]), (affected) => {
      this.toasts.show('success', describeBulkResult(verb, affected, 1));
      this.live.refreshNow();
    });
  }

  protected delete(): void {
    const id = this.message()?.id;
    if (!id) {
      return;
    }
    const queue = this.queueName();
    this.run(this.api.deleteMessages([id]), (affected) => {
      this.toasts.show('success', describeBulkResult('deleted', affected, 1));
      this.router.navigate(queue ? ['/queues', queue] : ['/queues']);
    });
  }

  private run(call: Observable<BulkActionResult>, done: (affected: number) => void): void {
    this.busy.set(true);
    call
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ affected }) => done(affected),
        error: (e: unknown) => this.toasts.show('error', describeError(e)),
      });
  }
}
