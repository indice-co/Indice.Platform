import { Output, EventEmitter, Input, ViewChild, TemplateRef } from '@angular/core';

import { TableColumn, DatatableComponent } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { SearchEvent } from './search-event';

export class ListView {
    // Properties.
    @ViewChild('dataTable', { static: true }) protected dataTable: DatatableComponent;
    @ViewChild('emailTemplate', { static: true }) public emailTemplate: TemplateRef<HTMLElement>;
    @ViewChild('phoneNumberTemplate', { static: true }) public phoneNumberTemplate: TemplateRef<HTMLElement>;
    @ViewChild('dateTimeTemplate', { static: true }) public dateTimeTemplate: TemplateRef<HTMLElement>;
    @ViewChild('booleanTemplate', { static: true }) public booleanTemplate: TemplateRef<HTMLElement>;
    @ViewChild('usernameTemplate', { static: true }) public usernameTemplate: TemplateRef<HTMLElement>;
    @Output() protected search: EventEmitter<SearchEvent> = new EventEmitter();
    @Input() public rows: any[];
    @Input() public rowsPerPage: number;
    @Input() public columns: TableColumn[];
    @Input() public count: number;
    @Input() public defaultSortField?: string;
    @Input() public defaultSortDirection?: SortDirection;
    @Input() public isLoading = false;
    @Input() public clientSide = false;
    @Input() public canFilter = false;
    public minimumSearchCharacters = 3;
    public searchTerm?: string;
    protected queryParamsSubscription: Subscription;
    protected page = 1;
    protected defaultRowsPerPage = 20;
    protected sortField?: string;
    protected sortDirection?: SortDirection;
}

export enum SortDirection {
    Asc = 'Asc',
    Desc = 'Desc'
}
