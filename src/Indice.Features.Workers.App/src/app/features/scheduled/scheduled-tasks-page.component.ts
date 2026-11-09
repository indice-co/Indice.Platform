import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { ScheduledTaskInfo, WorkersApiService } from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { clampProgress, describeCron } from './cron';
import { TaskEnabledToggleComponent } from './task-enabled-toggle.component';

/** A task row with the text the template would otherwise recompute on every poll. */
interface TaskRow {
  task: ScheduledTaskInfo;
  schedule: string | null;
  progress: number;
}

/** Hangfire's "Recurring jobs": every scheduled task with its last/next run and an Enabled switch. */
@Component({
  selector: 'app-scheduled-tasks-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    EmptyStateComponent,
    PageHeaderComponent,
    RelativeTimePipe,
    StatusBadgeComponent,
    TaskEnabledToggleComponent,
  ],
  template: `
    <section class="p-4 sm:p-6">
      <app-page-header title="Scheduled tasks" [subtitle]="subtitle()">
        <input
          type="search"
          class="input input-sm w-56"
          placeholder="Search name or description"
          aria-label="Search name or description"
          [value]="search()"
          (input)="onSearch($event)"
        />
        <select
          class="select select-sm w-auto"
          aria-label="Group"
          [value]="group()"
          (change)="onGroup($event)"
        >
          <option value="" [selected]="group() === ''">All groups</option>
          @for (g of groups(); track g) {
            <option [value]="g" [selected]="g === group()">{{ g }}</option>
          }
        </select>
      </app-page-header>

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft mb-4">{{ message }}</div>
      }

      <!-- A failed first load shows just the alert: an empty card would read as "no tasks". -->
      <div class="card bg-base-100 shadow-sm" [class.hidden]="tasks() === null && error()">
        @if (tasks() === null) {
          <div class="flex flex-col gap-2 p-4" aria-busy="true">
            @for (i of skeletonRows; track i) {
              <div class="skeleton h-8 w-full"></div>
            }
          </div>
        } @else if (tasks()!.length === 0) {
          <app-empty-state
            title="No scheduled tasks"
            text="Tasks appear here after their first run. The workers record each run in the database this dashboard reads."
          />
        } @else if (rows().length === 0) {
          <app-empty-state title="No tasks match the filter">
            <button type="button" class="btn btn-ghost btn-sm" (click)="clearFilters()">
              Clear filters
            </button>
          </app-empty-state>
        } @else {
          <div class="overflow-x-auto">
            <table class="table table-sm">
              <thead>
                <tr>
                  <th>Task</th>
                  @if (hasSchedules()) {
                    <th>Schedule</th>
                  }
                  <th>Last run</th>
                  <th>Next run</th>
                  <th class="text-right">Runs</th>
                  <th>Status</th>
                  <th>Enabled</th>
                </tr>
              </thead>
              <tbody>
                @for (row of rows(); track row.task.id) {
                  <tr
                    class="cursor-pointer hover:bg-base-200"
                    [class.opacity-60]="!row.task.enabled"
                    (click)="open(row.task)"
                  >
                    <td class="min-w-56">
                      <div class="flex flex-wrap items-center gap-1.5">
                        <a
                          class="font-medium hover:underline"
                          [routerLink]="['/scheduled', row.task.id]"
                          (click)="$event.stopPropagation()"
                          >{{ row.task.name }}</a
                        >
                        @if (row.task.group) {
                          <span class="badge badge-ghost badge-xs">{{ row.task.group }}</span>
                        }
                      </div>
                      @if (row.task.description) {
                        <p class="text-xs text-base-content/60">{{ row.task.description }}</p>
                      }
                    </td>
                    @if (hasSchedules()) {
                      <td>
                        @if (row.task.cronExpression) {
                          <span class="font-mono text-xs">{{ row.task.cronExpression }}</span>
                          @if (row.schedule) {
                            <p class="text-xs text-base-content/60">{{ row.schedule }}</p>
                          }
                        } @else {
                          <span class="text-base-content/40">—</span>
                        }
                      </td>
                    }
                    <td class="whitespace-nowrap">
                      @if (row.task.hasRun) {
                        <span
                          class="mr-1.5"
                          [title]="(row.task.lastExecution | date: 'medium') ?? ''"
                          >{{ row.task.lastExecution | relativeTime }}</span
                        >
                        @if (row.task.lastRunFailed) {
                          <app-status-badge
                            status="Failed"
                            [title]="'Failed ' + (row.task.lastErrorDate | date: 'medium')"
                          />
                        } @else {
                          <app-status-badge status="Ok" />
                        }
                      } @else {
                        <app-status-badge status="NotRun" />
                      }
                    </td>
                    <td
                      class="whitespace-nowrap"
                      [title]="(row.task.nextExecution | date: 'medium') ?? ''"
                    >
                      {{ row.task.nextExecution | relativeTime }}
                    </td>
                    <td class="text-right font-mono">{{ row.task.executionCount }}</td>
                    <td class="whitespace-nowrap">
                      <div class="flex items-center gap-2">
                        <app-status-badge [status]="row.task.status" />
                        @if (row.task.status === 'Running' && row.progress > 0) {
                          <progress
                            class="progress progress-warning w-16"
                            max="100"
                            [value]="row.progress"
                            [attr.aria-label]="row.progress + '% done'"
                          ></progress>
                        }
                      </div>
                    </td>
                    <td (click)="$event.stopPropagation()">
                      <app-task-enabled-toggle [task]="row.task" />
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </section>
  `,
})
export class ScheduledTasksPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly router = inject(Router);

  /** `null` until the first response, so the template can show skeletons instead of "empty". */
  protected readonly tasks = signal<ScheduledTaskInfo[] | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');
  protected readonly group = signal('');

  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  protected readonly groups = computed(() =>
    [...new Set((this.tasks() ?? []).map((t) => t.group).filter((g): g is string => !!g))].sort(),
  );

  /**
   * Cron expressions are only known inside the worker process, which this dashboard host is not —
   * so the column shows up only when the API does report one.
   */
  protected readonly hasSchedules = computed(() =>
    (this.tasks() ?? []).some((t) => !!t.cronExpression),
  );

  protected readonly subtitle = computed(() => {
    const tasks = this.tasks();
    if (!tasks) {
      return null;
    }
    const running = tasks.filter((t) => t.status === 'Running').length;
    const failing = tasks.filter((t) => t.lastRunFailed).length;
    return `${plural(tasks.length, 'task')} · ${running} running · ${failing} failing`;
  });

  protected readonly rows = computed<TaskRow[]>(() => {
    const term = this.search().trim().toLowerCase();
    const group = this.group();
    return (this.tasks() ?? [])
      .filter((t) => !group || t.group === group)
      .filter(
        (t) =>
          !term ||
          t.name.toLowerCase().includes(term) ||
          (t.description ?? '').toLowerCase().includes(term),
      )
      .map((task) => ({
        task,
        schedule: describeCron(task.cronExpression),
        progress: clampProgress(task.progress),
      }));
  });

  constructor() {
    this.live.tick$
      .pipe(
        switchMap(() =>
          this.api.getScheduledTasks().pipe(
            catchError((e) => {
              this.error.set(describeError(e));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((tasks) => {
        this.tasks.set(tasks);
        this.error.set(null);
        this.live.markUpdated();
      });
  }

  protected open(task: ScheduledTaskInfo): void {
    this.router.navigate(['/scheduled', task.id]);
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onGroup(event: Event): void {
    this.group.set((event.target as HTMLSelectElement).value);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.group.set('');
  }
}

function plural(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
