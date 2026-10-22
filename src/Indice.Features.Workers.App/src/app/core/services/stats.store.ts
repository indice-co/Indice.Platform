import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { describeError } from './api-error';
import { LiveRefreshService } from './live-refresh.service';
import { DashboardStats, WorkersApiService } from './workers-api.service';

/** How many `/stats` samples the "Live queue depth" chart keeps. */
const HISTORY_LENGTH = 60;

/**
 * Root store for `/stats`: the shell's nav badges and the Dashboard read it, so the endpoint is
 * polled once for the whole app. `history` is a client-side buffer — nothing is stored server-side.
 */
@Injectable({ providedIn: 'root' })
export class StatsStore {
  private readonly api = inject(WorkersApiService);
  private readonly live = inject(LiveRefreshService);

  /** Latest sample; `null` until the first response. */
  readonly stats = signal<DashboardStats | null>(null);
  readonly error = signal<string | null>(null);
  /** The last 60 samples, oldest first. */
  readonly history = signal<DashboardStats[]>([]);

  private started = false;

  /** Starts polling for the lifetime of `destroyRef` (the shell). Repeated calls are ignored. */
  start(destroyRef: DestroyRef): void {
    if (this.started) {
      return;
    }
    this.started = true;
    destroyRef.onDestroy(() => (this.started = false));
    this.live.tick$
      .pipe(
        switchMap(() =>
          this.api.getStats().pipe(
            catchError((error: unknown) => {
              this.error.set(describeError(error));
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((stats) => {
        this.stats.set(stats);
        this.history.update((list) => [...list, stats].slice(-HISTORY_LENGTH));
        this.error.set(null);
        this.live.markUpdated();
      });
  }
}
