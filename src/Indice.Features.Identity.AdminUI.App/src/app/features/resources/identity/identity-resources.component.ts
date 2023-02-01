import { Component, ViewChild, TemplateRef, OnInit } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityResourceInfo, IdentityApiService, IdentityResourceInfoResultSet } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';

@Component({
    selector: 'app-identity-resources',
    templateUrl: './identity-resources.component.html'
})
export class IdentityResourcesComponent implements OnInit {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;

    constructor(
        private _api: IdentityApiService,
        private _authService: AuthService
    ) { }

    public count = 0;
    public rows: IdentityResourceInfo[] = [];
    public columns: TableColumn[] = [];
    public canEditResource: boolean;

    public ngOnInit(): void {
        this.canEditResource = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'displayName', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getIdentityResources(event: SearchEvent): void {
        this._api.getIdentityResources(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((resources: IdentityResourceInfoResultSet) => {
            this.count = resources.count;
            this.rows = resources.items;
        });
    }
}
