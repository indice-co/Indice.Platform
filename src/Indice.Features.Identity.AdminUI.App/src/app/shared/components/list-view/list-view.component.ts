import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, TemplateRef, ViewChild, computed, input, linkedSignal, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { CellContext, PageEvent, SortPropDir, TableColumn } from '@swimlane/ngx-datatable';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs/operators';
import { SortDirection } from './models/list-view';
import { QueryParameters } from './models/query-parameters';
import { SearchEvent } from './models/search-event';

@Component({
    selector: 'app-list-view',
    templateUrl: './list-view.component.html',
    styleUrls: ['./list-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class ListViewComponent implements OnInit {
    // Cell templates handed to parent components. They are read in the parents' ngOnInit
    // (e.g. `cellTemplate: this._usersList.dateTimeTemplate`), so they must remain static queries.
    @ViewChild('emailTemplate', { static: true }) public emailTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('phoneNumberTemplate', { static: true }) public phoneNumberTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('dateTimeTemplate', { static: true }) public dateTimeTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('booleanTemplate', { static: true }) public booleanTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('usernameTemplate', { static: true }) public usernameTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('usernameOrEmailTemplate', { static: true }) public usernameOrEmailTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('keyTemplate', { static: true }) public keyTemplate: TemplateRef<CellContext<any>>;

    public readonly rows = input<any[]>([]);
    public readonly rowsPerPage = input<number>();
    public readonly columns = input<TableColumn[]>([]);
    public readonly count = input(0);
    public readonly defaultSortField = input<string>();
    public readonly defaultSortDirection = input<SortDirection>();
    public readonly isLoading = input(false);
    public readonly clientSide = input(false);
    public readonly canFilter = input(false);
    public readonly rowHeight = input(50);
    public readonly filter = input<any>(null);
    public readonly trackByProp = input<any>('id');
    public readonly search = output<SearchEvent>();

    public readonly minimumSearchCharacters = 3;
    public readonly searchTerm = signal<string | undefined>(undefined);
    private readonly defaultRowsPerPage = 20;
    protected readonly page = signal(1);
    protected readonly pageSize = linkedSignal(() => this.rowsPerPage() ?? this.defaultRowsPerPage);
    protected readonly sortField = signal<string | undefined>(undefined);
    protected readonly sortDirection = signal<SortDirection | undefined>(undefined);
    protected readonly sorts = computed<SortPropDir[]>(() => {
        const sortField = this.sortField();
        return sortField ? [{ prop: sortField, dir: this.sortDirection() === SortDirection.Desc ? 'desc' : 'asc' }] : [];
    });

    private readonly searchInput$ = new Subject<string>();
    private _filter: any;

    constructor(private route: ActivatedRoute, private router: Router, private destroyRef: DestroyRef) {
        this.searchInput$.pipe(
            filter(value => value.length >= this.minimumSearchCharacters || value.length === 0), // If character length greater than minimumSearchCharacters setting.
            debounceTime(1000), // Time in milliseconds between key events.
            distinctUntilChanged(), // If previous query is different from current.
            takeUntilDestroyed()
        ).subscribe(() => {
            this.page.set(1);
            this.setFilter();
        });
    }

    public ngOnInit(): void {
        const initialFilter = this.filter();
        if (initialFilter) {
            this._filter = { ...initialFilter };
        }
        this.route.queryParams.pipe(
            distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((params: Params) => {
            this.parseQueryParams(params);
            this.doSearch();
        });
    }

    /** ngx-datatable emits `page` for both pager clicks and header sorts; the event carries the new offset and the active sorts. */
    protected onPage(event: PageEvent): void {
        const page = event.offset + 1;
        const sort = event.sorts?.length ? event.sorts[event.sorts.length - 1] : undefined;
        const sortField = sort ? String(sort.prop) : undefined;
        const sortDirection = sort ? (sort.dir === 'desc' ? SortDirection.Desc : SortDirection.Asc) : undefined;
        if (page === this.page() && sortField === this.sortField() && sortDirection === this.sortDirection()) {
            return;
        }
        this.page.set(page);
        this.sortField.set(sortField);
        this.sortDirection.set(sortDirection);
        this.changeSearchLocation();
    }

    protected onSearchInput(value: string): void {
        this.searchTerm.set(value);
        this.searchInput$.next(value);
    }

    private setFilter(): void {
        if (this.searchTerm()?.length === 0) {
            this.searchTerm.set(undefined);
        }
        this.changeSearchLocation();
    }

    private parseQueryParams(params: Params): void {
        this.page.set(+(params[QueryParameters.PAGE] || 1));
        this.pageSize.set(+(params[QueryParameters.PAGE_SIZE] || this.pageSize()));
        this.sortField.set(params[QueryParameters.SORT_FIELD] || this.defaultSortField() || undefined);
        this.sortDirection.set((params[QueryParameters.SORT_DIRECTION] || this.defaultSortDirection() || undefined) as SortDirection);
        this.searchTerm.set(params[QueryParameters.SEARCH_TERM] || undefined);
        this.parseFilterParams(params);
    }

    private changeSearchLocation(): void {
        const params = {};
        params[QueryParameters.PAGE] = this.page();
        params[QueryParameters.PAGE_SIZE] = this.pageSize();
        params[QueryParameters.SORT_FIELD] = this.sortField() || this.defaultSortField() || undefined;
        params[QueryParameters.SORT_DIRECTION] = this.sortDirection();
        params[QueryParameters.SEARCH_TERM] = this.searchTerm();
        this.appendFilterParams(params);
        this.router.navigate([], { relativeTo: this.route, queryParams: params });
    }

    private doSearch(): void {
        const sortField = this.sortField();
        this.search.emit(new SearchEvent(
            this.page(),
            this.pageSize(),
            sortField ? `${sortField}${this.sortDirection() === SortDirection.Asc ? '+' : '-'}` : undefined,
            this.searchTerm() || undefined,
            this._filter
        ));
    }

    private parseFilterParams(params: Params) {
        if (!this._filter)
            return;
        Object.keys(this._filter).forEach(key => {
            if (params[key] !== this._filter[key]) {
                this._filter[key] = params[key]
            }
        })
    }

    private appendFilterParams(params: Params) {
        if (!this._filter)
            return;
        Object.keys(this._filter).forEach(key => {
            if (this._filter[key]) {
                params[key] = this._filter[key]
            }
        })
    }
}
