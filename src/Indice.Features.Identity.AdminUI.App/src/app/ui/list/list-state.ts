import { DestroyRef, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { BehaviorSubject, Observable, combineLatest, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';

export type SortDirection = 'asc' | 'desc';

export interface ListQuery {
  page: number;
  pageSize: number;
  sort: string;
  dir: SortDirection;
  search: string;
  /** Values of the filter params declared in `ListStateOptions.params` (e.g. `{ succeeded: 'false' }`). */
  params: Record<string, string | null>;
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
  /** Names of additional query params (filters) the list reacts to. */
  params?: string[];
}

/** The `field+` / `field-` sort expression understood by the Identity API list endpoints. */
export function toApiSort(query: Pick<ListQuery, 'sort' | 'dir'>): string | undefined {
  return query.sort ? `${query.sort}${query.dir === 'asc' ? '+' : '-'}` : undefined;
}

/** Reads a `'true'` / `'false'` filter param as a nullable boolean. */
export function toBool(value: string | null | undefined): boolean | undefined {
  return value === 'true' ? true : value === 'false' ? false : undefined;
}

/**
 * Signal based state for a paged, sortable, searchable list whose source of truth is the URL query string
 * (`page`, `pagesize`, `sort`, `dir`, `search` plus any declared filter params).
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
  public readonly params = signal<Record<string, string | null>>({});

  public readonly items = signal<T[] | null>(null);
  public readonly count = signal(0);
  public readonly loading = signal(false);
  public readonly failed = signal(false);

  public readonly pageCount = computed(() => Math.max(1, Math.ceil(this.count() / this.pageSize())));
  public readonly isEmpty = computed(() => !this.loading() && (this.items()?.length ?? 0) === 0);
  public readonly hasFilters = computed(() => this.search().length > 0 || Object.values(this.params()).some(v => v != null && v !== ''));

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
      params: this.params()
    };
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

  /** Sorting again by the same field toggles the direction. */
  public setSort(sort: string, dir?: SortDirection): void {
    const nextDir = dir ?? (this.sort() === sort && this.dir() === 'asc' ? 'desc' : 'asc');
    this.navigate({ page: 1, sort, dir: nextDir });
  }

  public setSearch(search: string): void {
    if ((search ?? '') === this.search()) {
      return;
    }
    this.navigate({ page: 1, search: search || null });
  }

  /** Sets declared filter params in a single navigation; `null`, `undefined` or empty removes a param. */
  public setParams(params: Record<string, string | number | boolean | null | undefined>): void {
    const queryParams: Params = { page: 1 };
    for (const [key, value] of Object.entries(params)) {
      queryParams[key] = value === undefined || value === null || value === '' ? null : String(value);
    }
    this.navigate(queryParams);
  }

  public clearFilters(): void {
    const queryParams: Params = { page: 1, search: null };
    for (const key of this._options.params ?? []) {
      queryParams[key] = null;
    }
    this.navigate(queryParams);
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
    const extra: Record<string, string | null> = {};
    for (const key of this._options.params ?? []) {
      extra[key] = params.get(key);
    }
    this.params.set(extra);
    return this.query;
  }
}
