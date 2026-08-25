import { Component, ViewChild, TemplateRef, OnInit } from '@angular/core';

import { CellContext, TableColumn } from '@swimlane/ngx-datatable';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, ApiScopeInfoResultSet, ApiScopeInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';

@Component({
    selector: 'app-scopes',
    templateUrl: './scopes.component.html',
    standalone: false
})
export class ScopesComponent implements OnInit {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('nameTemplate', { static: true }) public _nameTemplate: TemplateRef<CellContext<any>>;

    constructor(
        private _api: IdentityApiService,
        private _authService: AuthService
    ) { }

    public count = 0;
    public rows: ApiScopeInfo[] = [];
    public columns: TableColumn[] = [];
    public canEditScope: boolean;

    public ngOnInit(): void {
        this.canEditScope = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._nameTemplate },
            { prop: 'displayName', name: 'Display Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'name', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getScopes(event: SearchEvent): void {
        this._api.getApiScopes(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((scopes: ApiScopeInfoResultSet) => {
            this.count = scopes.count!;
            this.rows = scopes.items!;
        });
    }
}
