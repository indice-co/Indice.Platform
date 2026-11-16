import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  LucideArrowLeft,
  LucideCalendarClock,
  LucideCircleCheck,
  LucideCircleDashed,
  LucideCircleX,
  LucideDatabase,
  LucideTriangleAlert,
} from '@lucide/angular';
import { EMPTY, catchError, combineLatest, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
import { ScheduledTaskDetails, WorkersApiService } from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { JsonViewComponent } from '../../shared/json-view.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { formatDuration, shortTypeName } from '../queues/queue-status';
import { clampProgress, describeCron } from './cron';
import { RunTaskButtonComponent } from './run-task-button.component';
import { TaskEnabledToggleComponent } from './task-enabled-toggle.component';

/** One scheduled task: overview, the last error and its persisted state. */
@Component({
  selector: 'app-scheduled-task-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    LucideArrowLeft,
    LucideCalendarClock,
    LucideCircleCheck,
    LucideCircleDashed,
    LucideCircleX,
    LucideDatabase,
    LucideTriangleAlert,
    EmptyStateComponent,
    JsonViewComponent,
    RelativeTimePipe,
    RunTaskButtonComponent,
    StatusBadgeComponent,
    TaskEnabledToggleComponent,
  ],
  template: `
    <section class="p-4 sm:p-6">
      <div class="breadcrumbs pb-2 text-sm">
        <ul>
          <li><a routerLink="/scheduled">Scheduled tasks</a></li>
          <li class="text-base-content/70">{{ task()?.name ?? id() }}</li>
        </ul>
      </div>

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft mb-4">{{ message }}</div>
      }

      @if (notFound()) {
        <div class="card bg-base-100 shadow-sm">
          <app-empty-state
            title="Scheduled task not found"
            text="There is no scheduled task with this id in the database."
          >
            <a routerLink="/scheduled" class="btn btn-sm gap-1.5">
              <svg lucideArrowLeft class="size-4"></svg>Back to scheduled tasks
            </a>
          </app-empty-state>
        </div>
      } @else if (task(); as task) {
        <header class="flex flex-wrap items-center justify-between gap-3 pb-4">
          <div class="flex min-w-0 items-center gap-3">
            <span class="grid size-11 shrink-0 place-items-center rounded-box" [class]="chip()">
              <svg lucideCalendarClock class="size-5"></svg>
            </span>
            <div class="flex min-w-0 flex-wrap items-center gap-2">
              <h1 class="text-2xl font-semibold tracking-tight text-base-content break-all">
                {{ task.name }}
              </h1>
              @if (task.group) {
                <span class="badge badge-ghost badge-sm">{{ task.group }}</span>
              }
              <app-status-badge [status]="task.status" />
            </div>
          </div>
          <div class="flex flex-wrap items-center gap-4">
            @if (canManage()) {
              <app-run-task-button [task]="task" />
            }
            <label class="flex items-center gap-2 text-sm text-base-content/70">
              Enabled
              <app-task-enabled-toggle [task]="task" />
            </label>
          </div>
        </header>

        @if (!task.hasRun) {
          <div role="status" class="alert alert-info alert-soft mb-4">
            This task hasn't run yet.
          </div>
        }

        <div class="flex flex-col gap-4">
          <!-- Run facts first, then what the task is. Schedule and singleton come from the registered workers that host the task. -->
          <div class="card bg-base-100 shadow-sm">
            <div class="card-body gap-4 p-4">
              <dl class="grid grid-cols-2 gap-x-6 gap-y-4 lg:grid-cols-4">
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Last run</dt>
                  <dd class="text-sm">
                    @if (task.lastExecution) {
                      <span
                        class="flex items-center gap-1.5"
                        [title]="
                          task.status === 'Running'
                            ? 'Running since ' + (task.lastExecution | date: 'medium')
                            : task.lastRunFailed
                              ? 'Failed ' + (task.lastErrorDate | date: 'medium')
                              : 'Succeeded ' + (task.lastExecution | date: 'medium')
                        "
                      >
                        @if (task.status === 'Running') {
                          <svg lucideCircleDashed class="size-4 shrink-0 text-info" aria-label="Running"></svg>
                        } @else if (task.lastRunFailed) {
                          <svg lucideCircleX class="size-4 shrink-0 text-error" aria-label="Failed"></svg>
                        } @else {
                          <svg lucideCircleCheck class="size-4 shrink-0 text-success" aria-label="Succeeded"></svg>
                        }
                        {{ task.lastExecution | relativeTime }}
                      </span>
                      <span class="block text-xs text-base-content/70">{{
                        task.lastExecution | date: 'MMM d, y, HH:mm:ss'
                      }}</span>
                    } @else {
                      <span class="text-base-content/45">—</span>
                    }
                  </dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Next run</dt>
                  <dd class="text-sm">
                    @if (task.nextExecution) {
                      {{ task.nextExecution | relativeTime }}
                      <span class="block text-xs text-base-content/70">{{
                        task.nextExecution | date: 'MMM d, y, HH:mm:ss'
                      }}</span>
                    } @else {
                      <span class="text-base-content/45">—</span>
                    }
                  </dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Runs</dt>
                  <dd class="text-sm font-medium tabular-nums">{{ task.executionCount | number }}</dd>
                  @if (durations(); as durations) {
                    <dd
                      class="text-xs text-base-content/70 tabular-nums"
                      title="Measured by the live workers that host the task, since they started."
                    >
                      {{ durations }}
                    </dd>
                  }
                </div>
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Progress</dt>
                  <dd class="text-sm">
                    @if (progress() > 0) {
                      <span class="flex items-center gap-2">
                        <progress
                          class="progress w-24"
                          [class.progress-info]="task.status === 'Running'"
                          max="100"
                          [value]="progress()"
                          [attr.aria-label]="progress() + '% done'"
                        ></progress>
                        <span class="text-xs tabular-nums">{{ progress() }}%</span>
                      </span>
                    } @else {
                      <span class="text-base-content/45">—</span>
                    }
                  </dd>
                </div>
              </dl>

              <dl
                class="grid grid-cols-1 gap-x-6 gap-y-4 border-t border-base-200 pt-4 sm:grid-cols-2 lg:grid-cols-4"
              >
                @if (task.description) {
                  <div class="min-w-0 sm:col-span-2 lg:col-span-4">
                    <dt class="text-xs text-base-content/70">Description</dt>
                    <dd class="text-sm">{{ task.description }}</dd>
                  </div>
                }
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Id</dt>
                  <dd class="truncate font-mono text-sm" [title]="task.id">{{ task.id }}</dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Handler</dt>
                  <dd class="truncate font-mono text-sm" [title]="task.jobType ?? ''">
                    {{ shortType(task.jobType) || '—' }}
                  </dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-xs text-base-content/70">Last ran on</dt>
                  <dd class="truncate font-mono text-sm" [title]="task.workerId ?? ''">{{ task.workerId || '—' }}</dd>
                </div>
                @if (task.cronExpression) {
                  <div class="min-w-0">
                    <dt class="text-xs text-base-content/70">Schedule</dt>
                    <dd class="text-sm">
                      <span class="font-mono">{{ task.cronExpression }}</span>
                      @if (schedule()) {
                        <span class="block text-xs text-base-content/70">{{ schedule() }}</span>
                      }
                    </dd>
                  </div>
                }
                @if (task.singleton !== null && task.singleton !== undefined) {
                  <div class="min-w-0">
                    <dt class="text-xs text-base-content/70">Singleton</dt>
                    <dd class="text-sm">{{ task.singleton ? 'Yes' : 'No' }}</dd>
                  </div>
                }
              </dl>
            </div>
          </div>

          @if (task.errors) {
            <div class="card bg-base-100 shadow-sm">
              <div class="card-body">
                <div class="flex flex-wrap items-center gap-2">
                  <h2 class="card-title text-base">
                    <svg lucideTriangleAlert class="size-4 text-error"></svg>Last error
                  </h2>
                  <app-status-badge status="Failed" />
                  @if (task.lastErrorDate) {
                    <span class="text-sm text-base-content/70">
                      {{ task.lastErrorDate | date: 'medium' }}
                    </span>
                  }
                </div>
                <p class="text-sm text-base-content/70">
                  {{
                    task.lastRunFailed
                      ? 'Last run failed'
                      : 'An earlier run failed — the last run succeeded'
                  }}
                </p>
                <pre
                  class="app-scroll max-h-96 overflow-auto rounded-box bg-base-200 p-4 font-mono text-xs text-error"
                >{{ task.errors }}</pre>
              </div>
            </div>
          }

          <div class="card bg-base-100 shadow-sm">
            <div class="card-body">
              <h2 class="card-title text-base">
                <svg lucideDatabase class="size-4 text-base-content/70"></svg>State
              </h2>
              <app-json-view [json]="task.state" />
            </div>
          </div>
        </div>
      } @else if (!error()) {
        <div class="flex flex-col gap-3" aria-busy="true">
          <div class="skeleton h-9 w-72"></div>
          <div class="skeleton h-64 w-full"></div>
          <div class="skeleton h-40 w-full"></div>
        </div>
      }
    </section>
  `,
})
export class ScheduledTaskDetailPageComponent {
  /** Route param `:id` — the task id `{group}.{name}`, already decoded by the router. */
  readonly id = input.required<string>();

  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  protected readonly canManage = inject(PermissionsStore).canManage;

  protected readonly task = signal<ScheduledTaskDetails | null>(null);
  protected readonly notFound = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly schedule = computed(() => describeCron(this.task()?.cronExpression));
  protected readonly progress = computed(() => clampProgress(this.task()?.progress ?? 0));
  protected readonly shortType = shortTypeName;

  /** `avg 1.2 s · last 900 ms`, from the workers' own metrics; `null` when none reports them. */
  protected readonly durations = computed(() => {
    const task = this.task();
    const parts = [
      task?.averageDuration != null ? `avg ${formatDuration(task.averageDuration)}` : null,
      task?.lastDuration != null ? `last ${formatDuration(task.lastDuration)}` : null,
    ].filter((part): part is string => !!part);
    return parts.length ? parts.join(' · ') : null;
  });

  /** The task icon's tint: red when the last run failed, blue while it runs. Literal classes for Tailwind. */
  protected readonly chip = computed(() => {
    const task = this.task();
    if (task?.lastRunFailed) {
      return 'bg-error/10 text-error';
    }
    return task?.status === 'Running' ? 'bg-info/10 text-info' : 'bg-base-content/8 text-base-content/70';
  });

  constructor() {
    const id$ = toObservable(this.id).pipe(
      distinctUntilChanged(),
      // A reused component must not show the previous task while the next one loads.
      tap(() => {
        this.task.set(null);
        this.notFound.set(false);
        this.error.set(null);
      }),
    );
    combineLatest([id$, this.live.tick$])
      .pipe(
        switchMap(([id]) =>
          this.api.getScheduledTask(id).pipe(
            map((task): ScheduledTaskDetails | null => task),
            catchError((e) => {
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
      .subscribe((task) => {
        this.task.set(task);
        this.notFound.set(task === null);
        this.error.set(null);
        this.live.markUpdated();
      });
  }
}
