import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import {
  QueueInfo,
  QueueMessageStatus,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { COUNT_BADGE, countOf, plural, shortTypeName } from './queue-status';

/** The status columns, left to right. Dequeued only exists in the obsolete EF store. */
const COLUMNS: readonly QueueMessageStatus[] = ['Enqueued', 'Scheduled', 'Retrying', 'Poison'];

/**
 * Every logical queue with its waiting messages per status (`orders` and `orders-poison` are one
 * row). Only waiting work shows up: the relational store deletes a message once it is processed.
 */
@Component({
  selector: 'app-queues-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, EmptyStateComponent, PageHeaderComponent, RelativeTimePipe],
  template: `
    <section class="space-y-4 p-4 sm:p-6">
      <app-page-header title="Queues" [subtitle]="subtitle()">
        <label class="input input-sm w-56">
          <svg viewBox="0 0 24 24" fill="none" class="size-4 opacity-50" aria-hidden="true">
            <path
              d="M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14zM20 20l-4-4"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
            />
          </svg>
          <input
            type="search"
            placeholder="Filter queues"
            aria-label="Filter queues"
            [value]="filter()"
            (input)="filter.set($any($event.target).value)"
          />
        </label>
      </app-page-header>

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft">{{ message }}</div>
      }

      <div class="card bg-base-100 shadow-sm">
        @if (queues(); as all) {
          @if (visible().length) {
            <div class="overflow-x-auto">
              <table class="table table-sm">
                <thead>
                  <tr>
                    <th>Queue</th>
                    @for (status of columns; track status) {
                      <th class="text-right">{{ status }}</th>
                    }
                    <th>Oldest waiting</th>
                    @if (showWorkers()) {
                      <th>Workers</th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (queue of visible(); track queue.name) {
                    <tr class="cursor-pointer hover:bg-base-200" (click)="open(queue)">
                      <td class="max-w-xs">
                        <a
                          [routerLink]="['/queues', queue.name]"
                          class="block truncate font-medium"
                          [title]="queue.name"
                          (click)="$event.stopPropagation()"
                          >{{ queue.name }}</a
                        >
                        @if (handlerName(queue); as handler) {
                          <span
                            class="block truncate font-mono text-xs text-base-content/55"
                            [title]="queue.handlerType"
                            >{{ handler }}</span
                          >
                        }
                      </td>
                      @for (status of columns; track status) {
                        <td class="text-right tabular-nums">
                          @if (count(queue, status); as n) {
                            <span class="badge badge-sm" [class]="badge[status]">{{ n }}</span>
                          } @else {
                            <span class="text-base-content/40">0</span>
                          }
                        </td>
                      }
                      <td class="whitespace-nowrap text-base-content/70">
                        {{ queue.oldestEnqueuedAt | relativeTime }}
                      </td>
                      @if (showWorkers()) {
                        <td class="whitespace-nowrap text-base-content/70">
                          @if (queue.instanceCount != null && queue.pollingInterval != null) {
                            {{ queue.instanceCount }} × every {{ queue.pollingInterval }} ms
                          } @else {
                            —
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
              <div class="skeleton h-8 w-full"></div>
            }
          </div>
        }
      </div>
    </section>
  `,
})
export class QueuesPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly router = inject(Router);

  protected readonly columns = COLUMNS;
  protected readonly badge = COUNT_BADGE;
  protected readonly count = countOf;
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
}
