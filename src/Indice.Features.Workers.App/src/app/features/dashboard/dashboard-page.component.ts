import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { StatsStore } from '../../core/services/stats.store';
import {
  QueueInfo,
  ScheduledTaskInfo,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { SparklineChartComponent, SparklineSeries } from '../../shared/sparkline-chart.component';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { plural } from '../queues/queue-status';

interface StatCard {
  label: string;
  value: number;
  desc: string;
  route: string;
  /** Text colour of the value; empty keeps the default. */
  tone: string;
}

function card(label: string, value: number, desc: string, route: string, tone = ''): StatCard {
  return { label, value, desc, route, tone };
}

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
    RouterLink,
    EmptyStateComponent,
    PageHeaderComponent,
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

      <!-- Counters: messages on the first row, scheduled work on the second. -->
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        @for (card of cards(); track card.label) {
          <a
            [routerLink]="card.route"
            class="stats bg-base-100 shadow-sm transition-shadow hover:shadow-md"
          >
            <div class="stat">
              <div class="stat-title">{{ card.label }}</div>
              <div class="stat-value tabular-nums" [class]="card.tone">{{ card.value }}</div>
              <div class="stat-desc">{{ card.desc }}</div>
            </div>
          </a>
        } @empty {
          @for (i of skeletonCards; track i) {
            <div class="skeleton h-28 w-full"></div>
          }
        }
      </div>

      <div class="card bg-base-100 shadow-sm">
        <div class="card-body">
          <h2 class="card-title text-base">Live queue depth</h2>
          @if (depth(); as series) {
            <app-sparkline-chart [series]="series" />
          } @else {
            <div class="skeleton h-40 w-full"></div>
          }
          <p class="text-xs text-base-content/55">
            Sampled every {{ intervalSeconds() }} s in this browser — not stored.
          </p>
        </div>
      </div>

      <div class="grid gap-6 lg:grid-cols-2">
        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <div class="flex items-center justify-between gap-2">
              <h2 class="card-title text-base">Busiest queues</h2>
              <a routerLink="/queues" class="link link-hover text-sm text-primary">All queues</a>
            </div>
            @if (busiest(); as list) {
              @if (list.length) {
                <div class="overflow-x-auto">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Queue</th>
                        <th class="text-right">Enqueued</th>
                        <th class="text-right">Retrying</th>
                        <th class="text-right">Poison</th>
                        <th>Oldest</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (queue of list; track queue.name) {
                        <tr>
                          <td class="max-w-48 truncate">
                            <a
                              [routerLink]="['/queues', queue.name]"
                              class="link link-hover font-medium"
                              [title]="queue.name"
                              >{{ queue.name }}</a
                            >
                          </td>
                          <td class="text-right tabular-nums">{{ queue.enqueued }}</td>
                          <td
                            class="text-right tabular-nums"
                            [class.text-warning]="queue.retrying > 0"
                          >
                            {{ queue.retrying }}
                          </td>
                          <td
                            class="text-right tabular-nums"
                            [class.text-error]="queue.poison > 0"
                            [class.font-semibold]="queue.poison > 0"
                          >
                            {{ queue.poison }}
                          </td>
                          <td class="whitespace-nowrap text-base-content/70">
                            {{ queue.oldestEnqueuedAt | relativeTime }}
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
                  <div class="skeleton h-8 w-full"></div>
                }
              </div>
            }
          </div>
        </div>

        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <div class="flex items-center justify-between gap-2">
              <h2 class="card-title text-base">Tasks needing attention</h2>
              <a routerLink="/scheduled" class="link link-hover text-sm text-primary">All tasks</a>
            </div>
            @if (attention(); as list) {
              @if (list.length) {
                <ul class="divide-y divide-base-200">
                  @for (task of list; track task.id) {
                    <li>
                      <a
                        [routerLink]="['/scheduled', task.id]"
                        class="-mx-2 flex items-center gap-3 rounded-field px-2 py-2.5
                               hover:bg-base-200"
                      >
                        <span class="min-w-0 flex-1">
                          <span class="block truncate font-medium">{{ task.name }}</span>
                          <span class="block text-xs text-base-content/60">
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
                <app-empty-state title="All scheduled tasks are healthy" />
              }
            } @else {
              <div class="space-y-2">
                @for (i of skeletonRows; track i) {
                  <div class="skeleton h-8 w-full"></div>
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

  protected readonly skeletonCards = [1, 2, 3, 4, 5, 6, 7, 8];
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

  protected readonly intervalSeconds = computed(() => this.live.intervalMs() / 1000);

  /** From the shell's `StatsStore` poll — this page never calls `/stats` itself. */
  protected readonly cards = computed<StatCard[]>(() => {
    const s = this.statsStore.stats();
    if (!s) {
      return [];
    }
    const queues = '/queues';
    const tasks = '/scheduled';
    return [
      card('Enqueued', s.enqueued, `across ${plural(s.queues, 'queue')}`, queues, 'text-info'),
      card('Scheduled', s.scheduled, 'delayed messages', queues),
      card('Retrying', s.retrying, 'will retry', queues, 'text-warning'),
      card('Poison', s.poison, 'need attention', queues, 'text-error'),
      card('Scheduled tasks', s.scheduledTasks, `${s.disabledTasks} disabled`, tasks),
      card('Running', s.runningTasks, 'tasks running now', tasks, 'text-warning'),
      card('Failing', s.failingTasks, 'last run failed', tasks, 'text-error'),
      card('Active locks', s.activeLocks, `${plural(s.servers, 'server')} seen`, '/servers'),
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
}
