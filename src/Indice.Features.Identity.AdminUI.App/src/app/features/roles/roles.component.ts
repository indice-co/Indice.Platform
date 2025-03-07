import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, RoleInfoResultSet, RoleInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-roles',
    templateUrl: './roles.component.html'
})
export class RolesComponent implements OnInit {
    constructor(
        private _api: IdentityApiService,
        private _authService: AuthService
    ) { }

    @ViewChild('rolesList', { static: true }) public rolesList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('keyTemplate', { static: true }) public keyTemplate: TemplateRef<HTMLElement>;
    public count = 0;
    public rows: RoleInfo[] = [];
    public columns: TableColumn[] = [];
    public canEditRole: boolean;

    public ngOnInit(): void {
        this.canEditRole = this._authService.isAdminUIUsersWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this.keyTemplate },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this.actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getRoles(event: SearchEvent): void {
        this._api.getRoles(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((roles: RoleInfoResultSet) => {
            this.count = roles.count;
            this.rows = roles.items;
        });
    }
}
