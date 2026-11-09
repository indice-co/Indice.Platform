import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, catchError, combineLatest, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ScheduledTaskDetails, WorkersApiService } from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { JsonViewComponent } from '../../shared/json-view.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { clampProgress, describeCron } from './cron';
import { TaskEnabledToggleComponent } from './task-enabled-toggle.component';

/** One scheduled task: overview, the last error and its persisted state. */
@Component({
  selector: 'app-scheduled-task-detail-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    EmptyStateComponent,
    JsonViewComponent,
    RelativeTimePipe,
    StatusBadgeComponent,
    TaskEnabledToggleComponent,
  ],
  template: `
    <section class="p-4 sm:p-6">
      <div class="breadcrumbs pb-2 text-sm">
        <ul>
          <li><a routerLink="/scheduled">Scheduled tasks</a></li>
          <li class="text-base-content/60">{{ task()?.name ?? id() }}</li>
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
            <a routerLink="/scheduled" class="btn btn-ghost btn-sm">Back to scheduled tasks</a>
          </app-empty-state>
        </div>
      } @else if (task(); as task) {
        <header class="flex flex-wrap items-center justify-between gap-3 pb-4">
          <div class="flex min-w-0 flex-wrap items-center gap-2">
            <h1 class="text-2xl font-semibold tracking-tight text-base-content break-all">
              {{ task.name }}
            </h1>
            @if (task.group) {
              <span class="badge badge-ghost badge-sm">{{ task.group }}</span>
            }
            <app-status-badge [status]="task.status" />
          </div>
          <label class="flex items-center gap-2 text-sm text-base-content/70">
            Enabled
            <app-task-enabled-toggle [task]="task" />
          </label>
        </header>

        @if (!task.hasRun) {
          <div role="status" class="alert alert-info alert-soft mb-4">
            This task hasn't run yet.
          </div>
        }

        <div class="flex flex-col gap-4">
          <div class="card bg-base-100 shadow-sm">
            <div class="card-body">
              <h2 class="card-title text-base">Overview</h2>
              <dl class="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-[10rem_1fr]">
                <dt class="text-base-content/60">Id</dt>
                <dd class="font-mono break-all">{{ task.id }}</dd>

                <dt class="text-base-content/60">Handler</dt>
                <dd class="font-mono break-all">{{ task.jobType || '—' }}</dd>

                @if (task.description) {
                  <dt class="text-base-content/60">Description</dt>
                  <dd>{{ task.description }}</dd>
                }

                <!-- Schedule and singleton are only known inside the worker process. -->
                @if (task.cronExpression) {
                  <dt class="text-base-content/60">Schedule</dt>
                  <dd>
                    <span class="font-mono">{{ task.cronExpression }}</span>
                    @if (schedule()) {
                      <span class="text-base-content/60"> · {{ schedule() }}</span>
                    }
                  </dd>
                }

                @if (task.singleton !== null && task.singleton !== undefined) {
                  <dt class="text-base-content/60">Singleton</dt>
                  <dd>{{ task.singleton ? 'Yes' : 'No' }}</dd>
                }

                <dt class="text-base-content/60">Last ran on</dt>
                <dd class="font-mono">{{ task.workerId || '—' }}</dd>

                <dt class="text-base-content/60">Last run</dt>
                <dd>
                  @if (task.lastExecution) {
                    {{ task.lastExecution | date: 'medium' }}
                    <span class="text-base-content/60">
                      ({{ task.lastExecution | relativeTime }})</span
                    >
                  } @else {
                    —
                  }
                </dd>

                <dt class="text-base-content/60">Next run</dt>
                <dd>
                  @if (task.nextExecution) {
                    {{ task.nextExecution | date: 'medium' }}
                    <span class="text-base-content/60">
                      ({{ task.nextExecution | relativeTime }})</span
                    >
                  } @else {
                    —
                  }
                </dd>

                <dt class="text-base-content/60">Runs</dt>
                <dd class="font-mono">{{ task.executionCount }}</dd>

                <dt class="text-base-content/60">Progress</dt>
                <dd>
                  @if (progress() > 0) {
                    <span class="flex items-center gap-2">
                      <progress
                        class="progress w-40"
                        [class.progress-warning]="task.status === 'Running'"
                        max="100"
                        [value]="progress()"
                        [attr.aria-label]="progress() + '% done'"
                      ></progress>
                      <span class="font-mono text-xs">{{ progress() }}%</span>
                    </span>
                  } @else {
                    —
                  }
                </dd>
              </dl>
            </div>
          </div>

          @if (task.errors) {
            <div class="card bg-base-100 shadow-sm">
              <div class="card-body">
                <div class="flex flex-wrap items-center gap-2">
                  <h2 class="card-title text-base">Last error</h2>
                  <app-status-badge status="Failed" />
                  @if (task.lastErrorDate) {
                    <span class="text-sm text-base-content/70">
                      {{ task.lastErrorDate | date: 'medium' }}
                    </span>
                  }
                </div>
                <p class="text-sm text-base-content/60">
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
              <h2 class="card-title text-base">State</h2>
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

  protected readonly task = signal<ScheduledTaskDetails | null>(null);
  protected readonly notFound = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly schedule = computed(() => describeCron(this.task()?.cronExpression));
  protected readonly progress = computed(() => clampProgress(this.task()?.progress ?? 0));

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
