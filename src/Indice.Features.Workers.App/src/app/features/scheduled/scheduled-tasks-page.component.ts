import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import {
  LucideCalendarClock,
  LucideCircleCheck,
  LucideCircleDashed,
  LucideCircleX,
  LucideX,
} from '@lucide/angular';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
import { ScheduledTaskInfo, WorkersApiService } from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { PAGE_SIZES, PagerComponent } from '../../shared/pager.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';
import { SearchInputComponent } from '../../shared/search-input.component';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { clampProgress, describeCron } from './cron';
import { RunTaskButtonComponent } from './run-task-button.component';
import { TaskEnabledToggleComponent } from './task-enabled-toggle.component';

/** A task row with the text the template would otherwise recompute on every poll. */
interface TaskRow {
  task: ScheduledTaskInfo;
  schedule: string | null;
  progress: number;
  /** The task icon's tint: red when the last run failed, blue while it runs. */
  chip: string;
}

/** Literal class names, so Tailwind's scanner generates them. */
function taskChip(task: ScheduledTaskInfo): string {
  if (task.lastRunFailed) {
    return 'bg-error/10 text-error';
  }
  if (task.status === 'Running') {
    return 'bg-info/10 text-info';
  }
  return 'bg-base-content/8 text-base-content/70';
}

/** Hangfire's "Recurring jobs": every scheduled task with its last/next run and an Enabled switch. */
@Component({
  selector: 'app-scheduled-tasks-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    LucideCalendarClock,
    LucideCircleCheck,
    LucideCircleDashed,
    LucideCircleX,
    LucideX,
    EmptyStateComponent,
    PageHeaderComponent,
    PagerComponent,
    RelativeTimePipe,
    RunTaskButtonComponent,
    SearchInputComponent,
    StatusBadgeComponent,
    TaskEnabledToggleComponent,
  ],
  template: `
    <section class="p-4 sm:p-6">
      <app-page-header title="Scheduled tasks" [subtitle]="subtitle()">
        <app-search-input
          class="w-full sm:w-64"
          placeholder="Search name or description"
          [debounce]="150"
          [value]="search()"
          (search)="search.set($event)"
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
      <!-- overflow-hidden: the row hover would otherwise paint past the card's rounded corners. -->
      <div class="card overflow-hidden bg-base-100 shadow-sm" [class.hidden]="tasks() === null && error()">
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
            <button type="button" class="btn btn-sm gap-1.5" (click)="clearFilters()">
              <svg lucideX class="size-4"></svg>Clear filters
            </button>
          </app-empty-state>
        } @else {
          <div class="overflow-x-auto">
            <table class="table table-sm">
              <thead>
                <tr>
                  <th class="pl-4">Task</th>
                  @if (hasSchedules()) {
                    <th>Schedule</th>
                  }
                  <th>Last run</th>
                  <th>Next run</th>
                  <th class="text-right">Runs</th>
                  <th>Status</th>
                  <th [class.pr-4]="!canManage()">Enabled</th>
                  @if (canManage()) {
                    <th class="pr-4"><span class="sr-only">Actions</span></th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of pageRows(); track row.task.id) {
                  <tr
                    class="cursor-pointer hover:bg-base-200/60"
                    [class.opacity-70]="!row.task.enabled"
                    (click)="open(row.task)"
                  >
                    <td class="min-w-56 py-2.5 pl-4">
                      <div class="flex items-center gap-3">
                        <span class="grid size-8 shrink-0 place-items-center rounded-field" [class]="row.chip">
                          <svg lucideCalendarClock class="size-4"></svg>
                        </span>
                        <div class="min-w-0">
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
                            <p class="line-clamp-2 max-w-md text-xs text-base-content/70" [title]="row.task.description">
                              {{ row.task.description }}
                            </p>
                          }
                        </div>
                      </div>
                    </td>
                    @if (hasSchedules()) {
                      <td>
                        @if (row.task.cronExpression) {
                          <span class="font-mono text-xs">{{ row.task.cronExpression }}</span>
                          @if (row.schedule) {
                            <p class="text-xs text-base-content/70">{{ row.schedule }}</p>
                          }
                        } @else {
                          <span class="text-base-content/45">—</span>
                        }
                      </td>
                    }
                    <td class="whitespace-nowrap">
                      @if (row.task.hasRun) {
                        <div
                          class="flex items-center gap-2"
                          [title]="
                            row.task.status === 'Running'
                              ? 'Running since ' + (row.task.lastExecution | date: 'medium')
                              : row.task.lastRunFailed
                                ? 'Failed ' + (row.task.lastErrorDate | date: 'medium')
                                : 'Succeeded ' + (row.task.lastExecution | date: 'medium')
                          "
                        >
                          @if (row.task.status === 'Running') {
                            <svg lucideCircleDashed class="size-4 shrink-0 text-info" aria-label="Running"></svg>
                          } @else if (row.task.lastRunFailed) {
                            <svg lucideCircleX class="size-4 shrink-0 text-error" aria-label="Failed"></svg>
                          } @else {
                            <svg lucideCircleCheck class="size-4 shrink-0 text-success" aria-label="Succeeded"></svg>
                          }
                          <span>
                            {{ row.task.lastExecution | relativeTime }}
                            <span class="block text-xs text-base-content/70">{{
                              row.task.lastExecution | date: 'MMM d, HH:mm'
                            }}</span>
                          </span>
                        </div>
                      } @else {
                        <span class="flex items-center gap-2 text-base-content/70">
                          <svg lucideCircleDashed class="size-4 shrink-0" aria-hidden="true"></svg>Not run yet
                        </span>
                      }
                    </td>
                    <td class="whitespace-nowrap" [title]="(row.task.nextExecution | date: 'medium') ?? ''">
                      @if (row.task.nextExecution) {
                        {{ row.task.nextExecution | relativeTime }}
                        <span class="block text-xs text-base-content/70">{{
                          row.task.nextExecution | date: 'MMM d, HH:mm'
                        }}</span>
                      } @else {
                        <span class="text-base-content/45">—</span>
                      }
                    </td>
                    <td class="text-right font-medium tabular-nums">{{ row.task.executionCount | number }}</td>
                    <td class="whitespace-nowrap">
                      <div class="flex items-center gap-2">
                        <app-status-badge [status]="row.task.status" />
                        @if (row.task.status === 'Running' && row.progress > 0) {
                          <progress
                            class="progress progress-info w-16"
                            max="100"
                            [value]="row.progress"
                            [attr.aria-label]="row.progress + '% done'"
                          ></progress>
                        }
                      </div>
                    </td>
                    <td [class.pr-4]="!canManage()" (click)="$event.stopPropagation()">
                      <app-task-enabled-toggle [task]="row.task" />
                    </td>
                    @if (canManage()) {
                      <td class="pr-4 text-right" (click)="$event.stopPropagation()">
                        <app-run-task-button [task]="row.task" size="xs" />
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <!-- The endpoint returns every task, so paging happens here; a short list needs none. -->
          @if (rows().length > pageSizes[0]) {
            <app-pager
              class="border-t border-base-content/10 px-4 py-3"
              [page]="currentPage()"
              [size]="size()"
              [count]="rows().length"
              (pageChange)="page.set($event)"
              (sizeChange)="setSize($event)"
            />
          }
        }
      </div>
    </section>
  `,
})
export class ScheduledTasksPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly router = inject(Router);
  protected readonly canManage = inject(PermissionsStore).canManage;

  /** `null` until the first response, so the template can show skeletons instead of "empty". */
  protected readonly tasks = signal<ScheduledTaskInfo[] | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');
  protected readonly group = signal('');

  protected readonly skeletonRows = [1, 2, 3, 4, 5];
  protected readonly pageSizes = PAGE_SIZES;
  protected readonly size = signal<number>(PAGE_SIZES[0]);
  /** 1-based; back to the first page whenever the filter changes, but not on a poll. */
  protected readonly page = linkedSignal(() => {
    this.search();
    this.group();
    return 1;
  });

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
        chip: taskChip(task),
      }));
  });

  /** The requested page, pulled back when a poll or a bigger page size leaves it past the end. */
  protected readonly currentPage = computed(() =>
    Math.min(this.page(), Math.max(1, Math.ceil(this.rows().length / this.size()))),
  );
  protected readonly pageRows = computed(() => {
    const start = (this.currentPage() - 1) * this.size();
    return this.rows().slice(start, start + this.size());
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

  protected onGroup(event: Event): void {
    this.group.set((event.target as HTMLSelectElement).value);
  }

  protected setSize(size: number): void {
    this.size.set(size);
    this.page.set(1);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.group.set('');
  }
}

function plural(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
