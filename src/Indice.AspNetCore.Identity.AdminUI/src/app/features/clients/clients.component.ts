import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, ClientInfoResultSet, ClientInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';

@Component({
    selector: 'app-clients',
    templateUrl: './clients.component.html'
})
export class ClientsComponent implements OnInit {
    constructor(private _api: IdentityApiService) { }

    @ViewChild('clientsList', { static: true }) public clientsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public actionsTemplate: TemplateRef<HTMLElement>;
    public count = 0;
    public rows: ClientInfo[] = [];
    public columns: TableColumn[] = [];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'clientId', name: 'Client Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'clientName', name: 'Client Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'enabled', name: 'Enabled', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this.clientsList.booleanTemplate },
            { prop: 'clientId', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this.actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getClients(event: SearchEvent): void {
        this._api.getClients(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((clients: ClientInfoResultSet) => {
            this.count = clients.count;
            this.rows = clients.items;
        });
    }
}
