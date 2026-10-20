import { inject, Injectable } from '@angular/core';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasesApiService, CaseStatus, GroupByReportResult, ReportTag } from 'src/app/core/services/cases-api.service';
import { openRoute, RouteStop, TimeWindow } from './dashboard-math';

/** The filters a count query can combine (all optional). */
export interface CountQuery {
  window?: TimeWindow;
  checkpointTypeCodes?: string[];
  assignedTo?: string;
}

/** Checkpoint codes per lifecycle status; "open" means Submitted or In progress. */
export interface CheckpointCodes {
  open: string[];
  byStatus: Partial<Record<CaseStatus, string[]>>;
  /** The open checkpoints in lifecycle order, one per code. */
  route: RouteStop[];
}

/**
 * Dashboard figures built from the existing endpoints only: the case list returns just its count when asked for a page of
 * size 0, and the group-by reports supply the breakdowns. Every query goes through the same access rules as the list.
 */
@Injectable({ providedIn: 'root' })
export class DashboardMetricsService {
  private readonly api = inject(CasesApiService);
  private readonly caseTypeService = inject(CaseTypeService);

  /** The distinct checkpoint types the user can see, grouped by lifecycle status. */
  public checkpointCodes(): Observable<CheckpointCodes> {
    return this.caseTypeService.getCheckpointTypes(true).pipe(
      map(types => {
        const byStatus: Partial<Record<CaseStatus, string[]>> = {};
        for (const type of types) {
          if (type.status && type.code) {
            (byStatus[type.status] ??= []).push(type.code);
          }
        }
        return {
          open: [...(byStatus[CaseStatus.Submitted] ?? []), ...(byStatus[CaseStatus.InProgress] ?? [])],
          byStatus,
          route: openRoute(types)
        };
      })
    );
  }

  /** How many cases match. `null` when the query failed. */
  public count(query: CountQuery = {}): Observable<number | null> {
    if (query.checkpointTypeCodes && query.checkpointTypeCodes.length === 0) {
      return of(0);
    }
    return this.list(query, 0).pipe(
      map(result => result.count),
      catchError(() => of(null))
    );
  }

  /** Counts for several queries at once, in order. */
  public counts(queries: CountQuery[]): Observable<(number | null)[]> {
    return queries.length ? forkJoin(queries.map(query => this.count(query))) : of([]);
  }

  /** The longest-waiting open case, if any. */
  public oldestOpen(openCodes: string[]): Observable<CasePartial | null> {
    if (!openCodes.length) {
      return of(null);
    }
    return this.list({ checkpointTypeCodes: openCodes }, 1, 'createdByWhen').pipe(
      map(result => result.items[0] ?? null),
      catchError(() => of(null))
    );
  }

  /** A group-by report, or `null` when it failed. */
  public report(tag: ReportTag): Observable<GroupByReportResult[] | null> {
    return this.api.getCaseReport(tag).pipe(catchError(() => of(null)));
  }

  private list(query: CountQuery, size: number, sort?: string): Observable<{ count: number; items: CasePartial[] }> {
    return this.api.getCases(
      1,
      size,
      sort,
      undefined,
      [],
      [],
      [],
      query.window?.from,
      query.window?.to,
      undefined,
      [],
      // The list API takes these as filter clauses, exactly as the case list sends them.
      (query.checkpointTypeCodes ?? []).map(code => `checkpointTypeCodes::eq::${code}`),
      [],
      [],
      [],
      undefined,
      undefined,
      query.assignedTo
    ).pipe(map(result => ({ count: result.count ?? 0, items: result.items ?? [] })));
  }
}
