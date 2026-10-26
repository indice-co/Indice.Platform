import { signal } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { SearchSelectItem } from './search-select.component';

/** Search + "show more" paging state behind an `<app-search-select>`. */
export class PagedLookup<T> {
  public readonly items = signal<SearchSelectItem<T>[]>([]);
  public readonly loading = signal(false);
  public readonly hasMore = signal(false);
  private _page = 1;
  private _term = '';
  private _subscription?: Subscription;

  constructor(
    private readonly _fetch: (page: number, size: number, term: string) => Observable<{ items?: T[] | undefined }>,
    private readonly _map: (item: T) => SearchSelectItem<T>,
    private readonly _pageSize = 10
  ) { }

  public search(term: string): void {
    this._page = 1;
    this._term = term ?? '';
    this.load(false);
  }

  public more(): void {
    this._page++;
    this.load(true);
  }

  private load(append: boolean): void {
    this._subscription?.unsubscribe();
    this.loading.set(true);
    this._subscription = this._fetch(this._page, this._pageSize, this._term).subscribe({
      next: result => {
        const page = (result.items ?? []).map(this._map);
        this.items.update(items => append ? [...items, ...page] : page);
        this.hasMore.set(page.length === this._pageSize);
        this.loading.set(false);
      },
      error: () => {
        if (append) {
          this._page--;
        }
        this.loading.set(false);
      }
    });
  }
}
