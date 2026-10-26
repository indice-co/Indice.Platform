import { Injectable, inject } from '@angular/core';
import { Params } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { Observable, map, of, shareReplay } from 'rxjs';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import {
  CaseDashboardMetrics, CasePartial, CaseStatus, CasesApiService, GroupByReportResult, ReportTag
} from 'src/app/core/services/cases-api.service';
import { FilterClause, Operators } from 'src/app/ui/list/filter-clause';

/** Distinct checkpoint type codes per checkpoint status. */
export type StatusCodes = Record<CaseStatus, string[]>;

/** Days of daily intake requested from the server; the last 7 are "this week". */
export const INTAKE_DAYS = 14;
/** Open cases older than this many days count as stale. */
export const STALE_AFTER_DAYS = 7;

const DAY = 24 * 60 * 60 * 1000;

/**
 * Dashboard data. The figures come from the server's dashboard metrics endpoint (one call, same visibility rules as the
 * case list); checkpoint type codes are loaded only to build links into the case list with equivalent filters.
 */
@Injectable()
export class DashboardMetricsService {
  private readonly _api = inject(CasesApiService);
  private readonly _auth = inject(AuthService);
  private readonly _caseTypes = inject(CaseTypeService);

  public readonly statusCodes$: Observable<StatusCodes> = this._caseTypes.getCheckpointTypes().pipe(
    map(types => {
      const codes: StatusCodes = { Submitted: [], InProgress: [], Completed: [], Deleted: [] };
      for (const type of types) {
        if (type.status && type.code && !codes[type.status].includes(type.code)) {
          codes[type.status].push(type.code);
        }
      }
      return codes;
    }),
    shareReplay(1)
  );

  public get userId(): string | undefined {
    return this._auth.getSubjectId();
  }

  /**
   * Today as UTC midnight of the local calendar date, shifted by `offsetDays`. Used for date filters in list links:
   * the server compares on `DateTimeOffset.Date`, so a local midnight (e.g. 21:00Z for UTC+3) would shift ranges back a day.
   */
  public static day(offsetDays = 0): Date {
    const now = new Date();
    return new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()) + offsetDays * DAY);
  }

  public metrics(): Observable<CaseDashboardMetrics> {
    return this._api.getDashboardMetrics(INTAKE_DAYS, STALE_AFTER_DAYS);
  }

  /** The latest cases assigned to the current user; restricted to open checkpoints when the statuses are known. */
  public myQueue(openCodes: string[] | null): Observable<CasePartial[]> {
    const assignedTo = this.userId;
    if (!assignedTo || (openCodes && openCodes.length === 0)) {
      return of([]);
    }
    return this._api
      .getCases(1, 5, 'createdByWhen-', undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined,
        toCheckpointTerms(openCodes ?? undefined), undefined, undefined, undefined, undefined, undefined, undefined, assignedTo)
      .pipe(map(result => result.items ?? []));
  }

  public latest(): Observable<CasePartial[]> {
    return this._api.getCases(1, 5, 'createdByWhen-').pipe(map(result => result.items ?? []));
  }

  /** A server report, optionally limited to the cases of one channel. */
  public report(tag: ReportTag, channel?: string): Observable<GroupByReportResult[]> {
    return this._api.getCaseReport(tag, channel);
  }

  /** Query params that open the cases list with filters equivalent to a KPI. */
  public static casesLink(filters: FilterClause[]): Params {
    return { page: 1, pagesize: 20, sort: 'createdByWhen', dir: 'desc', filter: FilterClause.stringifyList(filters) };
  }

  public static codeFilters(codes: string[]): FilterClause[] {
    return codes.map(code => new FilterClause('checkpointTypeCodes', code, 'eq', 'array'));
  }

  public static fromFilter(date: Date): FilterClause {
    return new FilterClause('dateRange', date, Operators.GREATER_THAN_EQUAL.value, 'datetime');
  }

  public static toFilter(date: Date): FilterClause {
    return new FilterClause('dateRange', date, Operators.LESS_THAN_EQUAL.value, 'datetime');
  }

  public static assignedToFilter(userId: string): FilterClause {
    return new FilterClause('assignedTo', userId, 'eq', 'string');
  }
}

/** The API expects filter terms (`checkpointTypeCodes::eq::CODE`), not bare codes; a bare code is silently ignored. */
function toCheckpointTerms(codes: string[] | undefined): string[] | undefined {
  return codes?.map(code => `checkpointTypeCodes::eq::${code}`);
}
