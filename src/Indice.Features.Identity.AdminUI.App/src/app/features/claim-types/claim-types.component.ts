import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, ClaimTypeInfoResultSet, ClaimTypeInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-claim-types',
    templateUrl: './claim-types.component.html'
})
export class ClaimTypesComponent implements OnInit {
    @ViewChild('claimTypesList', { static: true }) public _claimTypesList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('valueTypeTemplate', { static: true }) public _valueTypeTemplate: TemplateRef<HTMLElement>;
    @ViewChild('nameTemplate', { static: true }) public _nameTemplate: TemplateRef<HTMLElement>;

    constructor(
        private _api: IdentityApiService,
        private _authService: AuthService
    ) { }

    public count = 0;
    public rows: ClaimTypeInfo[] = [];
    public columns: TableColumn[] = [];
    public canEditClaimType: boolean;

    public ngOnInit(): void {
        this.canEditClaimType = this._authService.isAdminUIUsersWriter() || this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._nameTemplate },
            { prop: 'valueType', name: 'Value Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._valueTypeTemplate },
            { prop: 'userEditable', name: 'User Editable', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._claimTypesList.booleanTemplate },
            { prop: 'required', name: 'Required', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._claimTypesList.booleanTemplate },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getClaimTypes(event: SearchEvent): void {
        this._api.getClaimTypes(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((claimTypes: ClaimTypeInfoResultSet) => {
            this.count = claimTypes.count;
            this.rows = claimTypes.items;
        });
    }
}
