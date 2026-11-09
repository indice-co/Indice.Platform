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
  search: string;
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

/**
 * Signal based state for a paged, sortable, filterable list whose source of truth is the URL query string
 * (`page`, `pagesize`, `sort`, `dir`, `search`, `filter`).
 *
 * Must be created in an injection context (a component field initializer or constructor). Call {@link start}
 * once everything `load` depends on is ready; from then on every query param change reloads the list.
 */
export class ListState<T> {
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _reload$ = new BehaviorSubject<void>(undefined);
  private _started = false;

  public readonly page = signal(1);
  public readonly pageSize: WritableSignal<number>;
  public readonly sort: WritableSignal<string>;
  public readonly dir: WritableSignal<SortDirection>;
  public readonly search = signal('');
  public readonly filters = signal<FilterClause[]>([]);

  public readonly items = signal<T[] | null>(null);
  public readonly count = signal(0);
  public readonly loading = signal(false);
  public readonly failed = signal(false);

  public readonly pageCount = computed(() => Math.max(1, Math.ceil(this.count() / this.pageSize())));
  public readonly isEmpty = computed(() => !this.loading() && (this.items()?.length ?? 0) === 0);
  public readonly hasFilters = computed(() => this.filters().length > 0 || this.search().length > 0);

  constructor(private readonly _options: ListStateOptions<T>) {
    this.pageSize = signal(_options.pageSize ?? 20);
    this.sort = signal(_options.sort ?? '');
    this.dir = signal(_options.dir ?? 'desc');
  }

  public get query(): ListQuery {
    return {
      page: this.page(),
      pageSize: this.pageSize(),
      sort: this.sort(),
      dir: this.dir(),
      search: this.search(),
      filters: this.filters()
    };
  }

  public get started(): boolean {
    return this._started;
  }

  /** Starts listening to the URL and loads the first page. Calling it again refreshes the list. */
  public start(): void {
    if (this._started) {
      this.refresh();
      return;
    }
    this._started = true;
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
        takeUntilDestroyed(this._destroyRef)
      )
      .subscribe(result => {
        this.items.set(result?.items ?? []);
        this.count.set(result?.count ?? 0);
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

  public setSort(sort: string, dir?: SortDirection): void {
    const nextDir = dir ?? (this.sort() === sort && this.dir() === 'desc' ? 'asc' : 'desc');
    this.navigate({ page: 1, sort, dir: nextDir });
  }

  public setSearch(search: string): void {
    if ((search ?? '') === this.search()) {
      return;
    }
    this.navigate({ page: 1, search: search || null });
  }

  public setFilters(filters: FilterClause[]): void {
    this.navigate({ page: 1, filter: FilterClause.stringifyList(filters) || null });
  }

  /** Changes filters and search in a single navigation (two merge navigations in a row would cancel each other). */
  public update(changes: { filters?: FilterClause[]; search?: string }): void {
    const queryParams: Params = { page: 1 };
    if (changes.filters) {
      queryParams['filter'] = FilterClause.stringifyList(changes.filters) || null;
    }
    if (changes.search !== undefined) {
      queryParams['search'] = changes.search || null;
    }
    this.navigate(queryParams);
  }

  /** Current query expressed as URL query params (useful for persisting / saving searches). */
  public toQueryParams(): Params {
    return {
      page: this.page(),
      pagesize: this.pageSize(),
      search: this.search() || undefined,
      sort: this.sort(),
      dir: this.dir(),
      filter: FilterClause.stringifyList(this.filters()) || undefined
    };
  }

  private navigate(queryParams: Params): void {
    this._router.navigate([], { relativeTo: this._route, queryParams, queryParamsHandling: 'merge' });
  }

  private readParams(params: ParamMap): ListQuery {
    this.page.set(Math.max(1, +(params.get('page') || 1)));
    this.pageSize.set(+(params.get('pagesize') || this._options.pageSize || 20));
    this.sort.set(params.get('sort') || this._options.sort || '');
    this.dir.set((params.get('dir') as SortDirection) || this._options.dir || 'desc');
    this.search.set(params.get('search') || '');
    this.filters.set(FilterClause.parseList(params.get('filter')));
    return this.query;
  }
}
