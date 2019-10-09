import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, ClaimTypeInfoResultSet, ClaimTypeInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';

@Component({
    selector: 'app-claim-types',
    templateUrl: './claim-types.component.html'
})
export class ClaimTypesComponent implements OnInit {
    constructor(private api: IdentityApiService) { }

    @ViewChild('claimTypesList', { static: true }) public claimTypesList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('valueTypeTemplate', { static: true }) public valueTypeTemplate: TemplateRef<HTMLElement>;
    @ViewChild('nameTemplate', { static: true }) public nameTemplate: TemplateRef<HTMLElement>;
    public count = 0;
    public rows: ClaimTypeInfo[] = [];
    public columns: TableColumn[] = [];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this.nameTemplate },
            { prop: 'valueType', name: 'Value Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this.valueTypeTemplate },
            { prop: 'userEditable', name: 'User Editable', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this.claimTypesList.booleanTemplate },
            { prop: 'required', name: 'Required', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this.claimTypesList.booleanTemplate },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this.actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getClaimTypes(event: SearchEvent): void {
        this.api.getClaimTypes(undefined, event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((claimTypes: ClaimTypeInfoResultSet) => {
            this.count = claimTypes.count;
            this.rows = claimTypes.items;
        });
    }
}
