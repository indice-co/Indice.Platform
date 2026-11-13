import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  LucideActivity,
  LucideArrowRight,
  LucideCalendarClock,
  LucideCircleCheck,
  LucideCircleX,
  LucideInbox,
  LucideLayers,
  LucideLock,
  LucidePlay,
} from '@lucide/angular';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService, formatInterval } from '../../core/services/live-refresh.service';
import { StatsStore } from '../../core/services/stats.store';
import {
  QueueInfo,
  QueueMessageStatus,
  ScheduledTaskInfo,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { SparklineChartComponent, SparklineSeries } from '../../shared/sparkline-chart.component';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { QueueMixComponent } from '../queues/queue-mix.component';
import { NEUTRAL_CHIP, STATUS_TONE, countOf, plural, queueChip } from '../queues/queue-status';
import { QueueStatusIconComponent } from '../queues/queue-status-icon.component';

/** The icon of a counter: a message status draws its own, the rest name theirs. */
type StatIcon = QueueMessageStatus | 'tasks' | 'running' | 'failing' | 'locks';

interface StatCard {
  label: string;
  value: number;
  desc: string;
  /** What the number counts, in full — the card's tooltip. */
  hint: string;
  route: string;
  icon: StatIcon;
  /** Status hue of the counter (`info`, `warning`, `error`…); empty for plain totals. */
  tone: string;
}

/** A labelled row of counters. */
interface StatGroup {
  key: 'messages' | 'work';
  title: string;
  desc: string;
  /** Empty until the first `/stats` response, so the row shows skeletons. */
  cards: StatCard[];
}

/** Literal class names, so Tailwind's scanner generates them. */
const TONE_TEXT: Record<string, string> = {
  info: 'text-info',
  secondary: 'text-secondary',
  warning: 'text-warning',
  error: 'text-error',
};
const TONE_CHIP: Record<string, string> = {
  info: 'bg-info/10 text-info',
  secondary: 'bg-secondary/10 text-secondary',
  warning: 'bg-warning/15 text-warning',
  error: 'bg-error/10 text-error',
  '': NEUTRAL_CHIP,
};

/** How many queues "Busiest queues" lists. */
const BUSIEST = 5;

/**
 * Landing page (Hangfire's Dashboard): the `/stats` counters, a live queue-depth chart built from
 * the polls this browser has made, and the two lists most likely to need a person — the busiest
 * queues and the failing or running tasks.
 */
@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    RouterLink,
    LucideActivity,
    LucideArrowRight,
    LucideCalendarClock,
    LucideCircleCheck,
    LucideCircleX,
    LucideInbox,
    LucideLayers,
    LucideLock,
    LucidePlay,
    EmptyStateComponent,
    PageHeaderComponent,
    QueueMixComponent,
    QueueStatusIconComponent,
    RelativeTimePipe,
    SparklineChartComponent,
    StatusBadgeComponent,
  ],
  template: `
    <section class="space-y-6 p-4 sm:p-6">
      <app-page-header title="Dashboard" subtitle="Live view of queues and scheduled tasks" />

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft">{{ message }}</div>
      }

      <!-- Counters: one labelled group for queue messages, one for scheduled work. -->
      @for (group of groups(); track group.key) {
        <div class="space-y-3">
          <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
            <h2 class="flex items-center gap-2 text-sm font-semibold text-base-content">
              <span class="grid size-6 place-items-center rounded-field bg-base-content/8 text-base-content/70">
                @if (group.key === 'messages') {
                  <svg lucideInbox class="size-3.5"></svg>
                } @else {
                  <svg lucideCalendarClock class="size-3.5"></svg>
                }
              </span>
              {{ group.title }}
            </h2>
            <span class="hidden h-4 w-px bg-base-content/15 sm:block" aria-hidden="true"></span>
            <p class="text-sm text-base-content/70">{{ group.desc }}</p>
          </div>
          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            @for (card of group.cards; track card.label) {
              <a
                [routerLink]="card.route"
                class="stats bg-base-100 shadow-sm transition-shadow hover:shadow-md"
                [title]="card.hint"
              >
                <div class="stat">
                  <!-- The hue only lights up when there is something to count; a calm board stays calm. -->
                  <div
                    class="stat-figure grid size-10 place-items-center rounded-field"
                    [class]="card.value > 0 ? toneChip[card.tone] : neutralChip"
                  >
                    @switch (card.icon) {
                      @case ('tasks') {
                        <svg lucideCalendarClock class="size-5"></svg>
                      }
                      @case ('running') {
                        <svg lucidePlay class="size-5"></svg>
                      }
                      @case ('failing') {
                        <svg lucideCircleX class="size-5"></svg>
                      }
                      @case ('locks') {
                        <svg lucideLock class="size-5"></svg>
                      }
                      @default {
                        <app-queue-status-icon class="size-5" [status]="$any(card.icon)" />
                      }
                    }
                  </div>
                  <div class="stat-title text-base-content/75">{{ card.label }}</div>
                  <div class="stat-value tabular-nums" [class]="card.value > 0 ? (toneText[card.tone] ?? '') : ''">
                    {{ card.value | number }}
                  </div>
                  <div class="stat-desc text-base-content/70">{{ card.desc }}</div>
                </div>
              </a>
            } @empty {
              @for (i of skeletonCards; track i) {
                <div class="skeleton h-28 w-full"></div>
              }
            }
          </div>
        </div>
      }

      <div class="card bg-base-100 shadow-sm">
        <div class="card-body">
          <h2 class="card-title text-base">
            <svg lucideActivity class="size-4 text-base-content/70"></svg>Live queue depth
          </h2>
          @if (depth(); as series) {
            <app-sparkline-chart [series]="series" />
          } @else {
            <div class="skeleton h-40 w-full"></div>
          }
          <p class="text-xs text-base-content/70">
            Sampled every {{ intervalLabel() }} in this browser — not stored.
          </p>
        </div>
      </div>

      <div class="grid gap-6 lg:grid-cols-2">
        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <div class="flex items-center justify-between gap-2">
              <h2 class="card-title text-base">
                <svg lucideLayers class="size-4 text-base-content/70"></svg>Busiest queues
              </h2>
              <a routerLink="/queues" class="btn btn-ghost btn-primary btn-xs gap-1">
                All queues<svg lucideArrowRight class="size-3.5"></svg>
              </a>
            </div>
            @if (busiest(); as list) {
              @if (list.length) {
                <div class="overflow-x-auto">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Queue</th>
                        <th>Messages</th>
                        @for (status of columns; track status) {
                          <th class="text-right">
                            <span class="inline-flex items-center gap-1.5">
                              <app-queue-status-icon class="size-3.5" [class]="tone[status].text" [status]="status" />
                              {{ status }}
                            </span>
                          </th>
                        }
                        <th>Oldest</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (queue of list; track queue.name) {
                        <tr class="hover:bg-base-200/60">
                          <td class="max-w-48 py-2.5">
                            <div class="flex min-w-0 items-center gap-2.5">
                              <span class="grid size-7 shrink-0 place-items-center rounded-field" [class]="queueChip(queue)">
                                <svg lucideLayers class="size-3.5"></svg>
                              </span>
                              <a
                                [routerLink]="['/queues', queue.name]"
                                class="truncate font-medium hover:underline"
                                [title]="queue.name"
                                >{{ queue.name }}</a
                              >
                            </div>
                          </td>
                          <td class="w-28 min-w-24">
                            <span class="text-sm font-semibold tabular-nums">{{ queue.total | number }}</span>
                            <app-queue-mix class="mt-1 w-20" [queue]="queue" [thin]="true" />
                          </td>
                          @for (status of columns; track status) {
                            <td class="text-right tabular-nums">
                              @if (countOf(queue, status); as n) {
                                <a
                                  [routerLink]="['/queues', queue.name]"
                                  [queryParams]="{ status }"
                                  class="font-semibold hover:underline"
                                  [class]="tone[status].text"
                                  >{{ n | number }}</a
                                >
                              } @else {
                                <span class="text-base-content/45">0</span>
                              }
                            </td>
                          }
                          <td class="whitespace-nowrap text-base-content/80">
                            @if (queue.oldestEnqueuedAt) {
                              {{ queue.oldestEnqueuedAt | relativeTime }}
                            } @else {
                              <span class="text-base-content/45">—</span>
                            }
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              } @else {
                <app-empty-state
                  title="No messages are waiting"
                  text="Processed messages are removed from the store."
                />
              }
            } @else {
              <div class="space-y-2">
                @for (i of skeletonRows; track i) {
                  <div class="skeleton h-10 w-full"></div>
                }
              </div>
            }
          </div>
        </div>

        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <div class="flex items-center justify-between gap-2">
              <h2 class="card-title text-base">
                <svg lucideCalendarClock class="size-4 text-base-content/70"></svg>Tasks needing attention
              </h2>
              <a routerLink="/scheduled" class="btn btn-ghost btn-primary btn-xs gap-1">
                All tasks<svg lucideArrowRight class="size-3.5"></svg>
              </a>
            </div>
            @if (attention(); as list) {
              @if (list.length) {
                <ul class="divide-y divide-base-content/10">
                  @for (task of list; track task.id) {
                    <li>
                      <a
                        [routerLink]="['/scheduled', task.id]"
                        class="-mx-2 flex items-center gap-3 rounded-field px-2 py-2.5
                               hover:bg-base-200/60"
                      >
                        <span
                          class="grid size-8 shrink-0 place-items-center rounded-field"
                          [class]="task.lastRunFailed ? 'bg-error/10 text-error' : 'bg-info/10 text-info'"
                        >
                          @if (task.lastRunFailed) {
                            <svg lucideCircleX class="size-4"></svg>
                          } @else {
                            <span class="loading loading-spinner loading-xs" aria-hidden="true"></span>
                          }
                        </span>
                        <span class="min-w-0 flex-1">
                          <span class="block truncate font-medium">{{ task.name }}</span>
                          <span class="block text-xs text-base-content/70">
                            @if (task.lastRunFailed) {
                              failed {{ task.lastErrorDate | relativeTime }}
                            } @else {
                              started {{ task.lastExecution | relativeTime }}
                            }
                          </span>
                        </span>
                        <app-status-badge [status]="task.lastRunFailed ? 'Failed' : 'Running'" />
                      </a>
                    </li>
                  }
                </ul>
              } @else {
                <app-empty-state title="All scheduled tasks are healthy">
                  <svg icon lucideCircleCheck class="text-success"></svg>
                </app-empty-state>
              }
            } @else {
              <div class="space-y-2">
                @for (i of skeletonRows; track i) {
                  <div class="skeleton h-10 w-full"></div>
                }
              </div>
            }
          </div>
        </div>
      </div>
    </section>
  `,
})
export class DashboardPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly statsStore = inject(StatsStore);

  protected readonly toneText = TONE_TEXT;
  protected readonly toneChip = TONE_CHIP;
  protected readonly neutralChip = NEUTRAL_CHIP;
  protected readonly tone = STATUS_TONE;
  /** The statuses that need a person; delayed and dequeued messages live on the queue page. */
  protected readonly columns: readonly QueueMessageStatus[] = ['Enqueued', 'Retrying', 'Poison'];
  protected readonly skeletonCards = [1, 2, 3, 4];
  protected readonly skeletonRows = [1, 2, 3, 4];

  /** `null` until the first response, so the template can show skeletons instead of "empty". */
  private readonly queues = signal<QueueInfo[] | null>(null);
  private readonly tasks = signal<ScheduledTaskInfo[] | null>(null);
  private readonly queuesError = signal<string | null>(null);
  private readonly tasksError = signal<string | null>(null);

  /** One banner for the three sources; each keeps its last good data on screen. */
  protected readonly error = computed(
    () => this.statsStore.error() ?? this.queuesError() ?? this.tasksError(),
  );

  protected readonly intervalLabel = computed(() => formatInterval(this.live.intervalMs()));

  /** From the shell's `StatsStore` poll — this page never calls `/stats` itself. */
  protected readonly groups = computed<StatGroup[]>(() => {
    const s = this.statsStore.stats();
    const queues = '/queues';
    const tasks = '/scheduled';
    return [
      {
        key: 'messages',
        title: 'Queue messages',
        desc: s
          ? `Waiting in ${plural(s.queues, 'queue')} right now — processed messages leave the store`
          : 'Waiting in the queues right now',
        cards: s
          ? [
              { label: 'Enqueued', value: s.enqueued, desc: 'ready for a worker', hint: 'Visible now, waiting for a worker to pick them up.', route: queues, icon: 'Enqueued', tone: 'info' },
              { label: 'Scheduled', value: s.scheduled, desc: 'delayed to a later time', hint: 'Added with a future "visible at" date; they become enqueued when it passes.', route: queues, icon: 'Scheduled', tone: 'secondary' },
              { label: 'Retrying', value: s.retrying, desc: 'failed, will try again', hint: 'Their handler threw; they go back to the queue until the retry limit is reached.', route: queues, icon: 'Retrying', tone: 'warning' },
              { label: 'Poison', value: s.poison, desc: 'gave up, need a person', hint: 'Out of retries or unreadable. They stay until someone requeues or deletes them.', route: queues, icon: 'Poison', tone: 'error' },
            ]
          : [],
      },
      {
        key: 'work',
        title: 'Scheduled tasks & locks',
        desc: s
          ? `Recurring jobs and the locks that keep each one on a single server · ${plural(s.servers, 'server')} seen`
          : 'Recurring jobs and the locks that keep each one on a single server',
        cards: s
          ? [
              { label: 'Scheduled tasks', value: s.scheduledTasks, desc: `${s.disabledTasks} disabled`, hint: 'Every recurring task that has recorded a run, enabled or not.', route: tasks, icon: 'tasks', tone: '' },
              { label: 'Running', value: s.runningTasks, desc: 'running right now', hint: 'Tasks whose current run has not finished yet.', route: tasks, icon: 'running', tone: 'info' },
              { label: 'Failing', value: s.failingTasks, desc: 'last run failed', hint: 'Tasks whose most recent run ended with an error.', route: tasks, icon: 'failing', tone: 'error' },
              { label: 'Active locks', value: s.activeLocks, desc: 'held by a server', hint: 'Distributed locks that are held and not yet expired.', route: '/servers', icon: 'locks', tone: '' },
            ]
          : [],
      },
    ];
  });

  /** Chart series from the client-side sample buffer; `null` until the first sample. */
  protected readonly depth = computed<SparklineSeries[] | null>(() => {
    const history = this.statsStore.history();
    if (!history.length) {
      return null;
    }
    return [
      { label: 'Enqueued', color: 'var(--color-info)', values: history.map((s) => s.enqueued) },
      { label: 'Retrying', color: 'var(--color-warning)', values: history.map((s) => s.retrying) },
      { label: 'Poison', color: 'var(--color-error)', values: history.map((s) => s.poison) },
    ];
  });

  protected readonly busiest = computed(() => {
    const queues = this.queues();
    return queues ? [...queues].sort((a, b) => b.total - a.total).slice(0, BUSIEST) : null;
  });

  /** Failing tasks first — a running task usually fixes itself, a failing one doesn't. */
  protected readonly attention = computed(() => {
    const tasks = this.tasks();
    if (!tasks) {
      return null;
    }
    return tasks
      .filter((t) => t.lastRunFailed || t.status === 'Running')
      .sort(
        (a, b) =>
          Number(b.lastRunFailed) - Number(a.lastRunFailed) || a.name.localeCompare(b.name),
      );
  });

  constructor() {
    // Two independent polls, so one failing endpoint doesn't blank the other card.
    this.live.tick$
      .pipe(
        switchMap(() =>
          this.api.getQueues().pipe(
            catchError((e: unknown) => {
              this.queuesError.set(describeError(e));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((queues) => {
        this.queues.set(queues);
        this.queuesError.set(null);
        this.live.markUpdated();
      });

    this.live.tick$
      .pipe(
        switchMap(() =>
          this.api.getScheduledTasks().pipe(
            catchError((e: unknown) => {
              this.tasksError.set(describeError(e));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((tasks) => {
        this.tasks.set(tasks);
        this.tasksError.set(null);
        this.live.markUpdated();
      });
  }

  protected readonly countOf = countOf;
  protected readonly queueChip = queueChip;
}
