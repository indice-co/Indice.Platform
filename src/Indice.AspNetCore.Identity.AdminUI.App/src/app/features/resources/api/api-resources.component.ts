import { Component, ViewChild, TemplateRef, OnInit } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, ApiResourceInfoResultSet, ApiResourceInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';

@Component({
    selector: 'app-api-resources',
    templateUrl: './api-resources.component.html'
})
export class ApiResourcesComponent implements OnInit {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('idTemplate', { static: true }) public _idTemplate: TemplateRef<HTMLElement>;

    constructor(private _api: IdentityApiService) { }

    public count = 0;
    public rows: ApiResourceInfo[] = [];
    public columns: TableColumn[] = [];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'name', name: 'Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._idTemplate },
            { prop: 'displayName', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getApiResources(event: SearchEvent): void {
        this._api.getApiResources(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((resources: ApiResourceInfoResultSet) => {
            this.count = resources.count;
            this.rows = resources.items;
        });
    }
}
