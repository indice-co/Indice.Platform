import { Params } from '@angular/router';
import { CaseStatus } from 'src/app/core/services/cases-api.service';

/** A closed time window used for the count queries (`from` ≤ created ≤ `to`). Either end may be open. */
export interface TimeWindow {
  from?: Date;
  to?: Date;
}

const DAY = 24 * 60 * 60 * 1000;

export function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function endOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999);
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/**
 * Rolling seven-day windows ending today, oldest first. The last window is "this week" (today and the six days before).
 */
export function weekWindows(today: Date, count: number): Required<TimeWindow>[] {
  return Array.from({ length: count }, (_, i) => {
    const weeksAgo = count - 1 - i;
    const to = endOfDay(addDays(today, -7 * weeksAgo));
    const from = startOfDay(addDays(to, -6));
    return { from, to };
  });
}

/** Age bands of open cases, youngest first; each band is the creation-time window that puts a case in it. */
export interface AgeBand extends TimeWindow {
  /** Translation key of the band label. */
  label: string;
}

export function ageBands(now: Date): AgeBand[] {
  const daysAgo = (days: number) => new Date(now.getTime() - days * DAY);
  return [
    { label: 'dashboard.age.underTwoDays', from: daysAgo(2) },
    { label: 'dashboard.age.twoToSevenDays', from: daysAgo(7), to: daysAgo(2) },
    { label: 'dashboard.age.sevenToThirtyDays', from: daysAgo(30), to: daysAgo(7) },
    { label: 'dashboard.age.overThirtyDays', to: daysAgo(30) }
  ];
}

/** Whole days between two instants (rounded down, never negative). */
export function daysBetween(from: Date, to: Date): number {
  return Math.max(0, Math.floor((to.getTime() - from.getTime()) / DAY));
}

/** Relative change in percent, rounded; `null` when there is no previous value to compare with. */
export function percentDelta(current: number, previous: number): number | null {
  if (!previous) {
    return null;
  }
  return Math.round(((current - previous) / previous) * 100);
}

/** Clean upper bound for a value axis and its two inner ticks (0, half, max). */
export function niceMax(value: number): number {
  if (value <= 4) {
    return 4;
  }
  const magnitude = Math.pow(10, Math.floor(Math.log10(value)));
  return [1, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10].map(s => s * magnitude).find(candidate => candidate >= value)!;
}

/** Case-list filters a dashboard number can open. */
export interface ListDrillDown {
  caseTypeCodes?: string[];
  checkpointTypeCodes?: string[];
  groupIds?: string[];
  window?: TimeWindow;
}

/**
 * The query string of the case list for a drill-down. Mirrors the list's own format (see `FilterClause.toString`), so
 * the list shows the filters as chips and the user can refine or save them.
 */
export function listQueryParams(drillDown: ListDrillDown): Params {
  const clauses = [
    ...(drillDown.caseTypeCodes ?? []).map(code => `caseTypeCodes::eq::${code}`),
    ...(drillDown.checkpointTypeCodes ?? []).map(code => `checkpointTypeCodes::eq::${code}`),
    ...(drillDown.groupIds ?? []).map(id => `groupIds::eq::${id}`),
    ...(drillDown.window?.from ? [`dateRange::gte::(datetime)${drillDown.window.from.toISOString()}`] : []),
    ...(drillDown.window?.to ? [`dateRange::lte::(datetime)${drillDown.window.to.toISOString()}`] : [])
  ];
  return {
    view: 'table',
    page: 1,
    pagesize: 10,
    search: '',
    sort: 'createdByWhen',
    dir: 'desc',
    filter: clauses.join(',')
  };
}

/** A checkpoint on the route open cases travel: Submitted checkpoints first, then the In progress ones. */
export interface RouteStop {
  code: string;
  title: string;
  status: CaseStatus;
}

/**
 * The open checkpoints in lifecycle order, one per code, keeping the order the API lists them in within a status (the
 * API does not promise a workflow order, so the route claims only Submitted → In progress).
 */
export function openRoute(types: { code?: string; title?: string; status?: CaseStatus }[]): RouteStop[] {
  const seen = new Set<string>();
  const stops: RouteStop[] = [];
  for (const status of [CaseStatus.Submitted, CaseStatus.InProgress]) {
    for (const type of types) {
      if (type.status === status && type.code && !seen.has(type.code)) {
        seen.add(type.code);
        stops.push({ code: type.code, title: type.title || type.code, status });
      }
    }
  }
  return stops;
}

/**
 * Keeps a route readable: past `limit` stops, the busiest `limit - 1` stay in route order and the rest fold into one
 * "other" stop at the end, so the stops still add up to the open total.
 */
export function foldRoute<T extends { value: number | null }>(stops: T[], limit: number, other: (folded: T[]) => T): T[] {
  if (stops.length <= limit) {
    return stops;
  }
  const keep = new Set([...stops].sort((a, b) => (b.value ?? 0) - (a.value ?? 0)).slice(0, limit - 1));
  return [...stops.filter(stop => keep.has(stop)), other(stops.filter(stop => !keep.has(stop)))];
}
