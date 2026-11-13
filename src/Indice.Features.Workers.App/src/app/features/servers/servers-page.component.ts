import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideLock, LucideLockOpen, LucideServer } from '@lucide/angular';
import { Observable, catchError, finalize, forkJoin, map, of, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import { PermissionsStore } from '../../core/services/permissions.store';
import { ToastService } from '../../core/services/toast.service';
import {
  LockInfo,
  WorkerServerInfo,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';

/** Hangfire's "Servers": the workers seen running scheduled tasks, and the distributed locks. */
@Component({
  selector: 'app-servers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    LucideLock,
    LucideLockOpen,
    LucideServer,
    ConfirmDialogComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    RelativeTimePipe,
  ],
  template: `
    <section class="p-4 sm:p-6">
      <app-page-header title="Servers & locks" [subtitle]="subtitle()" />

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft mb-4">{{ message }}</div>
      }

      <div class="flex flex-col gap-4">
        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <h2 class="card-title text-base">
              <svg lucideServer class="size-4 text-base-content/70"></svg>Servers
            </h2>
            <p class="text-sm text-base-content/70">
              Derived from scheduled task runs. Workers that only process queues don't appear here.
            </p>
            @if (servers(); as servers) {
              @if (servers.length === 0) {
                <app-empty-state
                  title="No servers yet"
                  text="A worker shows up here once it has run a scheduled task."
                />
              } @else {
                <div class="overflow-x-auto">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Worker</th>
                        <th>Last seen</th>
                        <th class="text-right">Tasks last run here</th>
                        <th class="text-right">Running now</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (server of servers; track server.workerId) {
                        <tr class="hover:bg-base-200/60">
                          <td class="py-2.5">
                            <div class="flex items-center gap-3">
                              <span
                                class="grid size-8 shrink-0 place-items-center rounded-field"
                                [class]="
                                  server.runningTasks > 0
                                    ? 'bg-info/10 text-info'
                                    : 'bg-base-content/8 text-base-content/70'
                                "
                              >
                                <svg lucideServer class="size-4"></svg>
                              </span>
                              <span class="font-mono break-all">{{ server.workerId }}</span>
                            </div>
                          </td>
                          <td class="whitespace-nowrap" [title]="(server.lastSeen | date: 'medium') ?? ''">
                            {{ server.lastSeen | relativeTime }}
                            <span class="block text-xs text-base-content/70">{{
                              server.lastSeen | date: 'MMM d, HH:mm'
                            }}</span>
                          </td>
                          <td class="text-right font-medium tabular-nums">{{ server.tasks }}</td>
                          <td class="text-right tabular-nums">
                            @if (server.runningTasks > 0) {
                              <span class="inline-flex items-center gap-1.5 font-semibold text-info">
                                <span class="loading loading-spinner loading-xs" aria-hidden="true"></span>
                                {{ server.runningTasks }}
                              </span>
                            } @else {
                              <span class="text-base-content/45">0</span>
                            }
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
            } @else if (!error()) {
              <div class="flex flex-col gap-2" aria-busy="true">
                <div class="skeleton h-8 w-full"></div>
                <div class="skeleton h-8 w-full"></div>
              </div>
            }
          </div>
        </div>

        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <h2 class="card-title text-base">
              <svg lucideLock class="size-4 text-base-content/70"></svg>Locks
            </h2>
            <p class="text-sm text-base-content/70">
              Distributed locks held by singleton scheduled tasks. Expired locks are cleaned up
              lazily.
            </p>
            @if (locks(); as locks) {
              @if (locks.length === 0) {
                <app-empty-state title="No locks are held" />
              } @else {
                <div class="overflow-x-auto">
                  <table class="table table-sm">
                    <thead>
                      <tr>
                        <th>Name</th>
                        <th>Expires</th>
                        <th class="text-right">Lease</th>
                        @if (canManage()) {
                          <th><span class="sr-only">Actions</span></th>
                        }
                      </tr>
                    </thead>
                    <tbody>
                      @for (lock of locks; track lock.id) {
                        <tr class="hover:bg-base-200/60">
                          <td class="py-2.5">
                            <div class="flex items-center gap-3">
                              <span
                                class="grid size-8 shrink-0 place-items-center rounded-field"
                                [class]="
                                  lock.isExpired
                                    ? 'bg-base-content/8 text-base-content/55'
                                    : 'bg-warning/15 text-warning'
                                "
                              >
                                @if (lock.isExpired) {
                                  <svg lucideLockOpen class="size-4"></svg>
                                } @else {
                                  <svg lucideLock class="size-4"></svg>
                                }
                              </span>
                              <span class="font-mono break-all" [class.text-base-content/70]="lock.isExpired">{{
                                lock.name
                              }}</span>
                            </div>
                          </td>
                          <td class="whitespace-nowrap" [title]="(lock.expirationDate | date: 'medium') ?? ''">
                            <span class="inline-flex items-center gap-1.5">
                              {{ lock.expirationDate | relativeTime }}
                              @if (lock.isExpired) {
                                <span class="badge badge-ghost badge-sm">expired</span>
                              }
                            </span>
                            <span class="block text-xs text-base-content/70">{{
                              lock.expirationDate | date: 'MMM d, HH:mm:ss'
                            }}</span>
                          </td>
                          <td class="whitespace-nowrap text-right tabular-nums">
                            {{ lock.durationSeconds }} s
                          </td>
                          @if (canManage()) {
                            <td class="text-right">
                              <button
                                type="button"
                                class="btn btn-soft btn-error btn-xs gap-1"
                                [disabled]="releasing() === lock.id"
                                (click)="askRelease(lock)"
                              >
                                @if (releasing() === lock.id) {
                                  <span class="loading loading-spinner loading-xs" aria-hidden="true"></span>
                                } @else {
                                  <svg lucideLockOpen class="size-3.5"></svg>
                                }
                                Release
                              </button>
                            </td>
                          }
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
            } @else if (!error()) {
              <div class="flex flex-col gap-2" aria-busy="true">
                <div class="skeleton h-8 w-full"></div>
                <div class="skeleton h-8 w-full"></div>
              </div>
            }
          </div>
        </div>
      </div>
    </section>

    <app-confirm-dialog
      #releaseDialog
      title="Release lock?"
      [message]="releaseMessage()"
      confirmLabel="Release"
      (confirmed)="release()"
    />
  `,
})
export class ServersPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);
  private readonly toasts = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly canManage = inject(PermissionsStore).canManage;
  private readonly releaseDialog = viewChild.required<ConfirmDialogComponent>('releaseDialog');

  /** The lock the confirm dialog is about. */
  private readonly pending = signal<LockInfo | null>(null);
  /** Id of the lock being released, for the button spinner. */
  protected readonly releasing = signal<string | null>(null);
  protected readonly releaseMessage = computed(() => {
    const lock = this.pending();
    return lock
      ? `Force-release "${lock.name}". If its task is still running, it is cancelled at its next lease renewal, and another worker may take the lock right away. Only do this for a lock left behind by a worker that died.`
      : '';
  });

  /** `null` until the first response, so the template can show skeletons instead of "empty". */
  protected readonly servers = signal<WorkerServerInfo[] | null>(null);
  protected readonly locks = signal<LockInfo[] | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly subtitle = computed(() => {
    const servers = this.servers();
    const locks = this.locks();
    if (!servers || !locks) {
      return null;
    }
    const active = locks.filter((l) => !l.isExpired).length;
    return `${plural(servers.length, 'server')} · ${plural(active, 'active lock')}`;
  });

  constructor() {
    // One poll for both cards. Each request fails on its own, so one card keeps its last good data
    // when the other call errors.
    this.live.tick$
      .pipe(
        switchMap(() =>
          forkJoin({
            servers: this.orFailure(this.api.getServers()),
            locks: this.orFailure(this.api.getLocks()),
          }),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ servers, locks }) => {
        if (!('failure' in servers)) {
          this.servers.set(servers.value);
        }
        if (!('failure' in locks)) {
          this.locks.set(locks.value);
        }
        const failure =
          'failure' in servers ? servers.failure : 'failure' in locks ? locks.failure : null;
        this.error.set(failure);
        if (failure === null) {
          this.live.markUpdated();
        }
      });
  }

  protected askRelease(lock: LockInfo): void {
    this.pending.set(lock);
    this.releaseDialog().open();
  }

  protected release(): void {
    const lock = this.pending();
    if (!lock) {
      return;
    }
    this.releasing.set(lock.id);
    this.api
      .releaseLock(lock.id)
      .pipe(
        finalize(() => this.releasing.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toasts.show('success', `Lock ${lock.name} released.`);
          this.live.refreshNow();
        },
        error: (e: unknown) => {
          if (e instanceof HttpErrorResponse && e.status === 404) {
            this.toasts.show('success', `Lock ${lock.name} was already released.`);
            this.live.refreshNow();
          } else {
            this.toasts.show('error', describeError(e));
          }
        },
      });
  }

  /** Turns an error into a value, so `forkJoin` still emits the call that succeeded. */
  private orFailure<T>(source: Observable<T>): Observable<{ value: T } | { failure: string }> {
    return source.pipe(
      map((value) => ({ value })),
      catchError((e) => of({ failure: describeError(e) })),
    );
  }
}

function plural(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
