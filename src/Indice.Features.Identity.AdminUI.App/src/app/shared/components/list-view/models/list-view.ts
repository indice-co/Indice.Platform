import { Output, EventEmitter, Input, ViewChild, TemplateRef, Component } from '@angular/core';

import { TableColumn, DatatableComponent, CellContext } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { SearchEvent } from './search-event';

@Component({
    template: '',
    standalone: false
})
export class ListView {
    // Properties.
    @ViewChild('dataTable', { static: true }) protected dataTable: DatatableComponent;
    @ViewChild('emailTemplate', { static: true }) public emailTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('phoneNumberTemplate', { static: true }) public phoneNumberTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('dateTimeTemplate', { static: true }) public dateTimeTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('booleanTemplate', { static: true }) public booleanTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('usernameTemplate', { static: true }) public usernameTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('usernameOrEmailTemplate', { static: true }) public usernameOrEmailTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('keyTemplate', { static: true }) public keyTemplate: TemplateRef<CellContext<any>>;
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
    @Input() public rowHeight = 50;
    @Input() public filter: any = null;
    @Input() public trackByProp: any = "id";
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
