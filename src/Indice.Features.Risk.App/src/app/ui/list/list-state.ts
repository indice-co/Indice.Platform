import { DestroyRef, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { BehaviorSubject, Observable, combineLatest, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import { FilterClause } from './filter-clause';

export type SortDirection = 'asc' | 'desc';

export interface ListQuery {
  page: number;
  pageSize: number;
  sort: string;
  dir: SortDirection;
  filters: FilterClause[];
}

export interface ResultSet<T> {
  count?: number;
  items?: T[] | undefined;
}

export interface ListStateOptions<T> {
  load: (query: ListQuery) => Observable<ResultSet<T> | null | undefined>;
  pageSize?: number;
  sort?: string;
  dir?: SortDirection;
}

/** The API's sort form: `createdAt` ascending, `createdAt-` descending. */
export function toApiSort(query: ListQuery): string | undefined {
  return query.sort ? (query.dir === 'asc' ? query.sort : `${query.sort}-`) : undefined;
}

/**
 * Signal based state for a paged, sortable, filterable list whose source of truth is the URL query string
 * (`page`, `pagesize`, `sort`, `dir`, `filter`). Create it in an injection context; it starts loading immediately.
 */
export class ListState<T> {
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  private readonly _reload$ = new BehaviorSubject<void>(undefined);

  public readonly page = signal(1);
  public readonly pageSize: WritableSignal<number>;
  public readonly sort: WritableSignal<string>;
  public readonly dir: WritableSignal<SortDirection>;
  public readonly filters = signal<FilterClause[]>([]);

  public readonly items = signal<T[] | null>(null);
  public readonly count = signal<number | null>(null);
  public readonly loading = signal(false);
  public readonly failed = signal(false);

  public readonly isEmpty = computed(() => !this.loading() && (this.items()?.length ?? 0) === 0);
  public readonly hasFilters = computed(() => this.filters().length > 0);

  constructor(private readonly _options: ListStateOptions<T>) {
    this.pageSize = signal(_options.pageSize ?? 20);
    this.sort = signal(_options.sort ?? '');
    this.dir = signal(_options.dir ?? 'desc');

    combineLatest([this._route.queryParamMap, this._reload$])
      .pipe(
        map(([params]) => this.readParams(params)),
        tap(() => {
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap(query => this._options.load(query).pipe(
          catchError(() => {
            this.failed.set(true);
            return of(null);
          })
        )),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe(result => {
        this.items.set(result?.items ?? []);
        this.count.set(result ? result.count ?? 0 : null);
        this.loading.set(false);
      });
  }

  public refresh(): void {
    this._reload$.next();
  }

  public setPage(page: number): void {
    this.navigate({ page });
  }

  public setPageSize(pageSize: number): void {
    this.navigate({ page: 1, pagesize: pageSize });
  }

  /** Sorts by `sort`; picking the current column again flips the direction. */
  public setSort(sort: string): void {
    const dir: SortDirection = this.sort() === sort && this.dir() === 'desc' ? 'asc' : 'desc';
    this.navigate({ page: 1, sort, dir });
  }

  public setFilters(filters: FilterClause[]): void {
    this.navigate({ page: 1, filter: FilterClause.stringifyList(filters) || null });
  }

  private navigate(queryParams: Params): void {
    this._router.navigate([], { relativeTo: this._route, queryParams, queryParamsHandling: 'merge' });
  }

  private readParams(params: ParamMap): ListQuery {
    this.page.set(Math.max(1, +(params.get('page') || 1)));
    this.pageSize.set(+(params.get('pagesize') || this._options.pageSize || 20));
    this.sort.set(params.get('sort') || this._options.sort || '');
    this.dir.set(params.get('dir') === 'asc' ? 'asc' : params.get('dir') === 'desc' ? 'desc' : this._options.dir || 'desc');
    this.filters.set(FilterClause.parseList(params.get('filter')));
    return { page: this.page(), pageSize: this.pageSize(), sort: this.sort(), dir: this.dir(), filters: this.filters() };
  }
}
