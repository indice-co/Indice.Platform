import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { LucideLayers, LucidePlus } from '@lucide/angular';
import { EMPTY, catchError, finalize, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
import { ToastService } from '../../core/services/toast.service';
import {
  QueueInfo,
  QueueMessageStatus,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { PayloadDialogComponent, PayloadDialogResult } from '../../shared/payload-dialog.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { SearchInputComponent } from '../../shared/search-input.component';
import { QueueMixComponent } from './queue-mix.component';
import { STATUS_TONE, countOf, formatPolling, plural, queueChip, shortTypeName } from './queue-status';
import { QueueStatusIconComponent } from './queue-status-icon.component';

/** The status columns in lifecycle order, as on the queue page. Dequeued only exists in the obsolete EF store. */
const COLUMNS: readonly QueueMessageStatus[] = ['Scheduled', 'Enqueued', 'Retrying', 'Poison'];

/**
 * Every logical queue with its waiting messages per status (`orders` and `orders-poison` are one
 * row). Only waiting work shows up: the relational store deletes a message once it is processed.
 */
@Component({
  selector: 'app-queues-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    LucideLayers,
    LucidePlus,
    EmptyStateComponent,
    PageHeaderComponent,
    PayloadDialogComponent,
    QueueMixComponent,
    QueueStatusIconComponent,
    RelativeTimePipe,
    SearchInputComponent,
  ],
  template: `
    <section class="space-y-4 p-4 sm:p-6">
      <app-page-header title="Queues" [subtitle]="subtitle()">
        <app-search-input
          class="w-full sm:w-56"
          placeholder="Filter queues"
          [debounce]="150"
          [value]="filter()"
          (search)="filter.set($event)"
        />
        @if (canManage()) {
          <button type="button" class="btn btn-sm btn-primary gap-1.5" (click)="payloadDialog.open()">
            <svg lucidePlus class="size-4"></svg>Add message
          </button>
        }
      </app-page-header>

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft">{{ message }}</div>
      }

      <div class="card overflow-hidden bg-base-100 shadow-sm">
        @if (queues(); as all) {
          @if (visible().length) {
            <div class="overflow-x-auto">
              <table class="table table-sm">
                <thead>
                  <tr>
                    <th class="pl-4">Queue</th>
                    <th>Messages</th>
                    @for (status of columns; track status) {
                      <th class="text-right">
                        <span class="inline-flex items-center gap-1.5">
                          <app-queue-status-icon class="size-3.5" [class]="tone[status].text" [status]="status" />
                          {{ status }}
                        </span>
                      </th>
                    }
                    <th>Oldest waiting</th>
                    @if (showWorkers()) {
                      <th class="pr-4">Workers</th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (queue of visible(); track queue.name) {
                    <tr class="cursor-pointer hover:bg-base-200/60" (click)="open(queue)">
                      <td class="max-w-xs py-2.5 pl-4">
                        <div class="flex min-w-0 items-center gap-3">
                          <span class="grid size-8 shrink-0 place-items-center rounded-field" [class]="chip(queue)">
                            <svg lucideLayers class="size-4"></svg>
                          </span>
                          <div class="min-w-0">
                            <a
                              [routerLink]="['/queues', queue.name]"
                              class="block truncate font-medium hover:underline"
                              [title]="queue.name"
                              (click)="$event.stopPropagation()"
                              >{{ queue.name }}</a
                            >
                            @if (handlerName(queue); as handler) {
                              <span
                                class="block truncate font-mono text-xs text-base-content/70"
                                [title]="queue.handlerType"
                                >{{ handler }}</span
                              >
                            }
                          </div>
                        </div>
                      </td>
                      <td class="w-36 min-w-32">
                        <span class="text-sm font-semibold tabular-nums">{{ queue.total | number }}</span>
                        <app-queue-mix class="mt-1 w-28" [queue]="queue" [thin]="true" />
                      </td>
                      @for (status of columns; track status) {
                        <td class="text-right tabular-nums">
                          @if (count(queue, status); as n) {
                            <a
                              [routerLink]="['/queues', queue.name]"
                              [queryParams]="{ status }"
                              class="font-semibold hover:underline"
                              [class]="tone[status].text"
                              [title]="'Open the ' + status.toLowerCase() + ' messages'"
                              (click)="$event.stopPropagation()"
                              >{{ n | number }}</a
                            >
                          } @else {
                            <span class="text-base-content/45">0</span>
                          }
                        </td>
                      }
                      <td
                        class="whitespace-nowrap text-base-content/80"
                        [title]="queue.oldestEnqueuedAt ? (queue.oldestEnqueuedAt | date: 'medium') : ''"
                      >
                        @if (queue.oldestEnqueuedAt) {
                          {{ queue.oldestEnqueuedAt | relativeTime }}
                        } @else {
                          <span class="text-base-content/45">—</span>
                        }
                      </td>
                      @if (showWorkers()) {
                        <td class="whitespace-nowrap pr-4 text-base-content/80">
                          @if (queue.instanceCount != null && queue.pollingInterval != null) {
                            {{ queue.instanceCount }} × every {{ polling(queue.pollingInterval) }}
                          } @else {
                            <span class="text-base-content/45">—</span>
                          }
                        </td>
                      }
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          } @else if (all.length) {
            <app-empty-state title="No queues match" [text]="noMatchText()" />
          } @else {
            <app-empty-state title="No messages are waiting" [text]="emptyText" />
          }
        } @else {
          <div class="space-y-2 p-4">
            @for (i of skeletonRows; track i) {
              <div class="skeleton h-10 w-full"></div>
            }
          </div>
        }
      </div>
    </section>

    <app-payload-dialog
      #payloadDialog
      title="Add a message"
      description="Pick an existing queue or type the name of a queue whose worker has no messages yet."
      hint="It must deserialize into the work item of the queue, or it ends up as poison."
      confirmLabel="Add message"
      confirmIcon="add"
      [askQueue]="true"
      [queues]="queueNames()"
      [askVisibleAt]="true"
      [busy]="busy()"
      (submitted)="enqueue($event)"
    />
  `,
})
export class QueuesPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly canManage = inject(PermissionsStore).canManage;
  protected readonly busy = signal(false);
  private readonly payloadDialog = viewChild.required<PayloadDialogComponent>('payloadDialog');

  protected readonly columns = COLUMNS;
  protected readonly tone = STATUS_TONE;
  protected readonly count = countOf;
  protected readonly polling = formatPolling;
  protected readonly chip = queueChip;
  protected readonly skeletonRows = [1, 2, 3, 4, 5];
  protected readonly emptyText =
    'Processed messages are removed from the store, so only waiting, delayed, retrying and ' +
    'poison messages appear here.';

  /** `null` until the first response, so the template can show skeletons instead of "empty". */
  protected readonly queues = signal<QueueInfo[] | null>(null);
  protected readonly error = signal<string | null>(null);
  /** Client-side name filter — the list is small and already loaded. */
  protected readonly filter = signal('');

  protected readonly visible = computed(() => {
    const term = this.filter().trim().toLowerCase();
    const queues = this.queues() ?? [];
    return term ? queues.filter((q) => q.name.toLowerCase().includes(term)) : queues;
  });
  protected readonly noMatchText = computed(() => `Nothing matches “${this.filter().trim()}”.`);
  protected readonly queueNames = computed(() => (this.queues() ?? []).map((q) => q.name));

  protected readonly subtitle = computed(() => {
    const queues = this.queues();
    if (!queues) {
      return null;
    }
    const total = queues.reduce((sum, q) => sum + q.total, 0);
    return `${plural(queues.length, 'queue')} · ${plural(total, 'message')} waiting`;
  });

  /**
   * Polling settings are only known inside a worker process, and the dashboard normally runs
   * elsewhere — so the column appears only when some queue actually reports them.
   */
  protected readonly showWorkers = computed(() =>
    (this.queues() ?? []).some((q) => q.instanceCount != null),
  );

  constructor() {
    this.live.tick$
      .pipe(
        switchMap(() =>
          this.api.getQueues().pipe(
            catchError((e: unknown) => {
              this.error.set(describeError(e));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((queues) => {
        this.queues.set(queues);
        this.error.set(null);
        this.live.markUpdated();
      });
  }

  protected handlerName(queue: QueueInfo): string | null {
    return shortTypeName(queue.handlerType);
  }

  protected open(queue: QueueInfo): void {
    this.router.navigate(['/queues', queue.name]);
  }

  protected enqueue(result: PayloadDialogResult): void {
    const queueName = result.queueName;
    if (!queueName) {
      return;
    }
    this.busy.set(true);
    this.api
      .enqueueMessage(queueName, { payload: result.payload, visibleAt: result.visibleAt })
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (message) => {
          this.payloadDialog().close();
          this.toasts.show('success', `Message added to ${queueName}.`);
          this.router.navigate(['/queues', queueName], { queryParams: { status: message.status } });
        },
        error: (e: unknown) => this.toasts.show('error', describeError(e)),
      });
  }
}
