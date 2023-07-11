import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';

import { fromEvent } from 'rxjs';
import { map, filter, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ListView, SortDirection } from './models/list-view';
import { QueryParameters } from './models/query-parameters';
import { SearchEvent } from './models/search-event';

@Component({
    selector: 'app-list-view',
    templateUrl: './list-view.component.html',
    styleUrls: ['./list-view.component.scss']
})
export class ListViewComponent extends ListView implements OnInit, OnDestroy {
    @ViewChild('filterInput', { static: true }) private _filterInput: ElementRef;

    private _filter: any;

    constructor(private route: ActivatedRoute, private router: Router) {
        super();
    }

    public ngOnInit(): void {
        if (this.filter) {
            this._filter = {};
            Object.keys(this.filter).forEach((key) => {
                this._filter[key] = this.filter[key]
            })
        }
        this.queryParamsSubscription = this.route.queryParams.subscribe((params: Params) => {
            this.parseQueryParams(params);
            this.doSearch();
        });
        fromEvent(this._filterInput.nativeElement, 'keyup').pipe(
            map((event: any) => {
                return event.target.value; // Get input value.
            }),
            filter(inputValue => inputValue.length >= this.minimumSearchCharacters || inputValue.length === 0), // If character length greater than minimumSearchCharacters setting.
            debounceTime(1000), // Time in milliseconds between key events.
            distinctUntilChanged() // If previous query is different from current.
        ).subscribe(_ => {
            this.page = 1;
            this.setFilter();
        });
    }

    public ngOnDestroy(): void {
        if (this.queryParamsSubscription) {
            this.queryParamsSubscription.unsubscribe();
        }
    }

    public setPage(event: { count: number, limit: number, offset: number, pageSize: number }): void {
        this.page = event.offset + 1;
        this.changeSearchLocation();
    }

    public setSort(event: any): void {
        const sort = event.sorts[event.sorts.length - 1];
        this.sortField = sort.prop;
        this.sortDirection = sort.dir === 'asc' ? SortDirection.Asc : SortDirection.Desc;
        this.changeSearchLocation();
    }

    private setFilter() {
        if (this.searchTerm.length === 0) {
            this.searchTerm = undefined;
        }
        this.changeSearchLocation();
    }

    private parseQueryParams(params: Params): void {
        this.page = +(params[QueryParameters.PAGE] || 1);
        this.dataTable.offset = this.page - 1;
        this.rowsPerPage = +(params[QueryParameters.PAGE_SIZE] || this.rowsPerPage || this.defaultRowsPerPage)
        this.sortField = params[QueryParameters.SORT_FIELD] || this.defaultSortField || undefined;
        this.sortDirection = (params[QueryParameters.SORT_DIRECTION] || this.defaultSortDirection || undefined) as SortDirection;
        if (this.sortField) {
            this.dataTable.sorts.splice(0, this.dataTable.sorts.length);
            this.dataTable.sorts.push({ prop: this.sortField, dir: this.sortDirection.toLowerCase() || 'asc' });
        }
        this.searchTerm = params[QueryParameters.SEARCH_TERM] || undefined;
        this.parseFilterParams(params)
    }

    private changeSearchLocation(): void {
        const params = {};
        params[QueryParameters.PAGE] = this.page;
        params[QueryParameters.PAGE_SIZE] = this.rowsPerPage || this.defaultRowsPerPage;
        params[QueryParameters.SORT_FIELD] = this.sortField || this.defaultSortField || undefined;
        params[QueryParameters.SORT_DIRECTION] = this.sortDirection;
        params[QueryParameters.SEARCH_TERM] = this.searchTerm;
        this.appendFilterParams(params);
        this.router.navigate([], { relativeTo: this.route, queryParams: params });
    }

    private doSearch(): void {
        this.search.emit(new SearchEvent(
            this.page,
            this.rowsPerPage,
            this.sortField ? `${this.sortField}${this.sortDirection === SortDirection.Asc ? '+' : '-'}` : undefined,
            this.searchTerm || undefined,
            this._filter
        ));
    }

    private isEmptyObject(object: any): boolean {
        return Object.entries(object).length === 0 && object.constructor === Object;
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
