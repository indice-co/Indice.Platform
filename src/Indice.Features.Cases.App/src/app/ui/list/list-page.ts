import { DestroyRef, Directive, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Observable, Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs/operators';
import { FilterClause, IResultSet, parseFilters, QueryParameters, SearchOption, stringifyFilters } from './filtering';

export interface SortOption {
  label: string;
  value: string;
}

/**
 * State and URL synchronisation of a paged list. The page, page size, search text, sort and filters are mirrored in the
 * query string (`view`, `page`, `pagesize`, `search`, `sort`, `dir`, `filter`) so a list can be bookmarked, saved as a
 * search and restored. Subclasses implement {@link loadItems} and call {@link startList} once their search options are known.
 */
@Directive()
export abstract class ListPage<T> {
  protected readonly route = inject(ActivatedRoute);
  protected readonly router = inject(Router);
  protected readonly destroyRef = inject(DestroyRef);

  /** The current page; `null` until the first load completes. Previous results stay visible while reloading. */
  public readonly items = signal<T[] | null>(null);
  public readonly count = signal(0);
  public readonly loading = signal(false);
  public readonly failed = signal(false);
  public readonly page = signal(1);
  public readonly pageSize = signal(20);
  public readonly sort = signal<string | null>('');
  public readonly sortdir = signal<string | null>('');
  public readonly search = signal<string | null>('');
  public readonly filters = signal<FilterClause[]>([]);
  public readonly searchOptions = signal<SearchOption[]>([]);
  public readonly sortOptions = signal<SortOption[]>([]);
  public view = 'table';
  public minimumSearchCharacters = 3;
  public searchDebounceTime = 300;

  private readonly searchText$ = new Subject<string | null>();
  private routeSub: Subscription | undefined;
  private searchSub: Subscription | undefined;
  private loadSub: Subscription | undefined;

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.routeSub?.unsubscribe();
      this.searchSub?.unsubscribe();
      this.loadSub?.unsubscribe();
    });
  }

  public abstract loadItems(): Observable<IResultSet<T> | null | undefined>;

  /** Reads the list state from the query string, normalises the URL and loads the first page. */
  protected startList(autoLoad = true): void {
    this.searchSub?.unsubscribe();
    this.searchSub = this.searchText$.pipe(
      filter(value => (value?.length ?? 0) >= this.minimumSearchCharacters || (value?.length ?? 0) === 0),
      debounceTime(this.searchDebounceTime),
      distinctUntilChanged()
    ).subscribe(text => {
      this.search.set(text);
      this.page.set(1);
      this.syncRoute();
      this.load();
    });
    this.routeSub?.unsubscribe();
    this.routeSub = this.route.queryParamMap.subscribe(params => this.readRoute(params));
    this.syncRoute(true);
    if (autoLoad) {
      this.load();
    }
  }

  public refresh(): void {
    this.page.set(1);
    this.syncRoute();
    this.load();
  }

  public pageChanged(page: number): void {
    if (this.page() !== page) {
      this.page.set(page);
      this.syncRoute();
      this.load();
    }
  }

  public pageSizeChanged(pageSize: number): void {
    if (this.pageSize() !== pageSize) {
      this.pageSize.set(pageSize);
      this.refresh();
    }
  }

  public sortChanged(sort: string): void {
    if (this.sort() !== sort) {
      this.sort.set(sort);
      this.refresh();
    }
  }

  public sortdirChanged(sortdir: string): void {
    if (this.sortdir() !== sortdir) {
      this.sortdir.set(sortdir);
      this.refresh();
    }
  }

  public advancedSearchChanged(filters: FilterClause[]): void {
    this.filters.set([...filters]);
    this.refresh();
  }

  public searchChanged(text: string | null): void {
    this.searchText$.next(text);
  }

  protected load(): void {
    this.loadSub?.unsubscribe();
    this.loading.set(true);
    this.failed.set(false);
    this.loadSub = this.loadItems().subscribe({
      next: result => {
        this.count.set(result?.count ?? 0);
        this.items.set(result?.items ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.count.set(0);
        this.items.set([]);
        this.failed.set(true);
        this.loading.set(false);
      }
    });
  }

  private readRoute(params: ParamMap): void {
    if (params.keys.length === 0) {
      return;
    }
    this.view = params.get(QueryParameters.VIEW) || this.view;
    this.page.set(+(params.get(QueryParameters.PAGE) || 1));
    this.pageSize.set(+(params.get(QueryParameters.PAGE_SIZE) || this.pageSize()));
    const search = params.get(QueryParameters.SEARCH);
    if (search !== null) {
      this.search.set(search);
    }
    const sort = params.get(QueryParameters.SORT_FIELD);
    if (sort) {
      this.sort.set(sort);
    }
    const dir = params.get(QueryParameters.SORT_DIRECTION);
    if (dir) {
      this.sortdir.set(dir);
    }
    this.filters.set(parseFilters(params.get(QueryParameters.FILTER), this.searchOptions()));
  }

  private syncRoute(skipLocationChange = false): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        view: this.view,
        page: this.page(),
        pagesize: this.pageSize(),
        search: this.search(),
        sort: this.sort(),
        dir: this.sortdir(),
        filter: stringifyFilters(this.filters())
      },
      queryParamsHandling: 'merge',
      skipLocationChange
    });
  }
}
