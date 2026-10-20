import { Injectable, inject } from '@angular/core';
import { Params } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { Observable, forkJoin, map, of, shareReplay, switchMap } from 'rxjs';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CaseStatus, CasesApiService, GroupByReportResult, ReportTag } from 'src/app/core/services/cases-api.service';
import { FilterClause, Operators } from 'src/app/ui/list/filter-clause';

/** Distinct checkpoint type codes per checkpoint status. */
export type StatusCodes = Record<CaseStatus, string[]>;

export interface CountQuery {
  from?: Date;
  to?: Date;
  /** Checkpoint type codes (ORed by the server). An empty array is treated as "matches nothing". */
  codes?: string[];
  assignedTo?: string;
}

export interface PipelineCounts {
  submitted: number;
  inProgress: number;
  completed: number;
  codes: StatusCodes;
}

export interface IntakeDay {
  date: Date;
  count: number;
}

const DAY = 24 * 60 * 60 * 1000;

/**
 * Dashboard KPIs computed on the client from `size=0` list calls (the API returns only `count`).
 *
 * Dates are sent as UTC midnight of the local calendar day: the server compares on `DateTimeOffset.Date`,
 * so a local midnight (e.g. 21:00Z for UTC+3) would shift every range back one day.
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

  /** Today as UTC midnight of the local calendar date, shifted by `offsetDays`. */
  public static day(offsetDays = 0): Date {
    const now = new Date();
    return new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()) + offsetDays * DAY);
  }

  public count(query: CountQuery): Observable<number> {
    if (query.codes && query.codes.length === 0) {
      return of(0); // an empty code list would mean "no filter" to the API
    }
    return this._api
      .getCases(1, 0, undefined, undefined, undefined, undefined, undefined, query.from, query.to, undefined, undefined,
        toCheckpointTerms(query.codes), undefined, undefined, undefined, undefined, undefined, query.assignedTo)
      .pipe(map(result => result.count ?? 0));
  }

  public pipeline(): Observable<PipelineCounts> {
    return this.statusCodes$.pipe(
      switchMap(codes => forkJoin({
        submitted: this.count({ codes: codes.Submitted }),
        inProgress: this.count({ codes: codes.InProgress }),
        completed: this.count({ codes: codes.Completed })
      }).pipe(map(counts => ({ ...counts, codes }))))
    );
  }

  /** Cases submitted per day for the last `days` days, oldest first, ending today. */
  public intake(days = 14): Observable<IntakeDay[]> {
    const dates = Array.from({ length: days }, (_, i) => DashboardMetricsService.day(i - days + 1));
    return forkJoin(dates.map(date => this.count({ from: date, to: date }))).pipe(
      map(counts => counts.map((count, i) => ({ date: dates[i], count })))
    );
  }

  /** Cases assigned to the current user; restricted to open checkpoints when the statuses are known. */
  public myQueue(openCodes: string[] | null): Observable<{ count: number; items: CasePartial[] }> {
    const assignedTo = this.userId;
    if (!assignedTo || (openCodes && openCodes.length === 0)) {
      return of({ count: 0, items: [] });
    }
    return this._api
      .getCases(1, 5, 'createdByWhen-', undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined,
        toCheckpointTerms(openCodes ?? undefined), undefined, undefined, undefined, undefined, undefined, assignedTo)
      .pipe(map(result => ({ count: result.count ?? 0, items: result.items ?? [] })));
  }

  public latest(): Observable<CasePartial[]> {
    return this._api.getCases(1, 5, 'createdByWhen-').pipe(map(result => result.items ?? []));
  }

  public report(tag: ReportTag): Observable<GroupByReportResult[]> {
    return this._api.getCaseReport(tag);
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

  public static assignedToFilter(userId: string): FilterClause {
    return new FilterClause('assignedTo', userId, 'eq', 'string');
  }
}

/** The API expects filter terms (`checkpointTypeCodes::eq::CODE`), not bare codes; a bare code is silently ignored. */
function toCheckpointTerms(codes: string[] | undefined): string[] | undefined {
  return codes?.map(code => `checkpointTypeCodes::eq::${code}`);
}
