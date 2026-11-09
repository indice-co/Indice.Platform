import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';

import { describeError } from '../../core/services/api-error';
import { LiveRefreshService } from '../../core/services/live-refresh.service';
import {
  LockInfo,
  WorkerServerInfo,
  WorkersApiService,
} from '../../core/services/workers-api.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { PageHeaderComponent } from '../../shared/page-header.component';
import { RelativeTimePipe } from '../../shared/relative-time.pipe';

/** Hangfire's "Servers": the workers seen running scheduled tasks, and the distributed locks. */
@Component({
  selector: 'app-servers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, EmptyStateComponent, PageHeaderComponent, RelativeTimePipe],
  template: `
    <section class="p-4 sm:p-6">
      <app-page-header title="Servers & locks" [subtitle]="subtitle()" />

      @if (error(); as message) {
        <div role="alert" class="alert alert-error alert-soft mb-4">{{ message }}</div>
      }

      <div class="flex flex-col gap-4">
        <div class="card bg-base-100 shadow-sm">
          <div class="card-body">
            <h2 class="card-title text-base">Servers</h2>
            <p class="text-xs text-base-content/60">
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
                        <tr>
                          <td class="font-mono break-all">{{ server.workerId }}</td>
                          <td
                            class="whitespace-nowrap"
                            [title]="(server.lastSeen | date: 'medium') ?? ''"
                          >
                            {{ server.lastSeen | relativeTime }}
                          </td>
                          <td class="text-right font-mono">{{ server.tasks }}</td>
                          <td class="text-right">
                            @if (server.runningTasks > 0) {
                              <span class="badge badge-warning badge-sm">{{
                                server.runningTasks
                              }}</span>
                            } @else {
                              <span class="font-mono text-base-content/40">0</span>
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
            <h2 class="card-title text-base">Locks</h2>
            <p class="text-xs text-base-content/60">
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
                      </tr>
                    </thead>
                    <tbody>
                      @for (lock of locks; track lock.id) {
                        <tr [class.opacity-60]="lock.isExpired">
                          <td class="font-mono break-all">{{ lock.name }}</td>
                          <td
                            class="whitespace-nowrap"
                            [title]="(lock.expirationDate | date: 'medium') ?? ''"
                          >
                            {{ lock.expirationDate | relativeTime }}
                            @if (lock.isExpired) {
                              <span class="badge badge-ghost badge-sm ml-1">expired</span>
                            }
                          </td>
                          <td class="text-right font-mono whitespace-nowrap">
                            {{ lock.durationSeconds }} s
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
      </div>
    </section>
  `,
})
export class ServersPageComponent {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);

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
