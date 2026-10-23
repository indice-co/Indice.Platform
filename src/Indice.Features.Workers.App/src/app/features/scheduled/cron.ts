import { signal } from '@angular/core';

type CronDescriber = (expression: string) => string;

/**
 * cronstrue, once loaded. It is ~23 kB and the API only reports cron expressions when it runs in
 * the worker process — which the dashboard host normally is not — so it loads on first use only.
 */
const describer = signal<CronDescriber | null>(null);
let loading: Promise<void> | null = null;

/**
 * Human text for a Quartz cron expression ("At 02:00"). Quartz has a seconds field; cronstrue
 * reads 6–7 parts. `null` for no expression, and until cronstrue has loaded: called from a
 * `computed`, it re-runs by itself when the text becomes available.
 */
export function describeCron(expression: string | null | undefined): string | null {
  if (!expression) {
    return null;
  }
  const describe = describer();
  if (!describe) {
    void loadCronstrue();
    return null;
  }
  return describe(expression) || null;
}

/** Loads cronstrue once; resolves when `describeCron` can answer. */
export function loadCronstrue(): Promise<void> {
  loading ??= import('cronstrue')
    .then(({ default: cronstrue }) =>
      describer.set((expression) =>
        cronstrue.toString(expression, {
          use24HourTimeFormat: true,
          throwExceptionOnParseError: false,
        }),
      ),
    )
    .catch(() => {
      // Offline or a stale deploy: the raw expression is still shown. Try again next time.
      loading = null;
    });
  return loading;
}

/** `progress` is reported by the task itself and has no fixed scale; treat it as a percentage. */
export function clampProgress(progress: number): number {
  return Math.min(100, Math.max(0, progress));
}
