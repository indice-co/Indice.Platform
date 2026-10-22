import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import {
  EMPTY,
  Observable,
  Subject,
  combineLatest,
  fromEvent,
  map,
  merge,
  of,
  startWith,
  switchMap,
  timer,
} from 'rxjs';

const LIVE_KEY = 'workers.live';
const INTERVAL_KEY = 'workers.interval';

/** The poll intervals the top-bar control offers, in ms. */
export const REFRESH_INTERVALS = [2000, 5000, 10000, 30000] as const;

/** Global poll clock (Hangfire-style). Pages combine `tick$` with their own query and refetch on every emission. */
@Injectable({ providedIn: 'root' })
export class LiveRefreshService {
  /** Auto-refresh on/off — remembered in localStorage `workers.live`. Default true. */
  readonly enabled = signal(readBool(LIVE_KEY, true));
  /** Poll interval in ms — one of 2000/5000/10000/30000, remembered in `workers.interval`. Default 5000. */
  readonly intervalMs = signal(readInterval(INTERVAL_KEY, 5000));
  /** When the last successful refresh landed (shown as "Updated 3 s ago"). */
  readonly lastUpdated = signal<Date | null>(null);

  private readonly manual$ = new Subject<void>();
  private readonly visible$ = fromEvent(document, 'visibilitychange').pipe(
    map(() => !document.hidden),
    startWith(!document.hidden),
  );

  /**
   * NOT shared: every subscriber gets its own timer, so a freshly opened page always loads once.
   * Visible + live → timer(0, ms); visible + paused → a single emission; hidden → nothing.
   */
  readonly tick$: Observable<void> = merge(
    combineLatest([toObservable(this.enabled), toObservable(this.intervalMs), this.visible$]).pipe(
      switchMap(([on, ms, visible]) => (!visible ? EMPTY : on ? timer(0, ms) : of(0))),
    ),
    this.manual$,
  ).pipe(map(() => undefined));

  setEnabled(on: boolean): void {
    this.enabled.set(on);
    write(LIVE_KEY, String(on));
  }

  setInterval(ms: number): void {
    if (!isKnownInterval(ms)) {
      return;
    }
    this.intervalMs.set(ms);
    write(INTERVAL_KEY, String(ms));
  }

  refreshNow(): void {
    this.manual$.next();
  }

  markUpdated(): void {
    this.lastUpdated.set(new Date());
  }
}

function isKnownInterval(ms: number): boolean {
  return (REFRESH_INTERVALS as readonly number[]).includes(ms);
}

/** Storage can be unavailable (private browsing, blocked site data) — fall back to the default. */
function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // The preference just won't survive the reload.
  }
}

function readBool(key: string, fallback: boolean): boolean {
  const value = read(key);
  return value === null ? fallback : value === 'true';
}

function readInterval(key: string, fallback: number): number {
  const value = Number(read(key));
  return isKnownInterval(value) ? value : fallback;
}
