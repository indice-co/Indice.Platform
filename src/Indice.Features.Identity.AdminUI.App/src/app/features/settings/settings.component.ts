import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { CellContext, TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, AppSettingInfo, AppSettingInfoResultSet } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';

@Component({
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    standalone: false
})
export class SettingsComponent implements OnInit {
    @ViewChild('actionsTemplate', { static: true }) public _actionsTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('valueTemplate', { static: true }) public _valueTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('keyTemplate', { static: true }) public _keyTemplate: TemplateRef<CellContext<any>>;

    constructor(private _api: IdentityApiService) { }

    public count = 0;
    public rows: AppSettingInfo[] = [];
    public columns: TableColumn[] = [];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'key', name: 'Name', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._keyTemplate },
            { prop: 'value', name: 'Value', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._valueTemplate },
            { prop: 'key', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getSettings(event: SearchEvent): void {
        this._api.getSettings(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((settings: AppSettingInfoResultSet) => {
            this.count = settings.count;
            this.rows = settings.items;
        });
    }
}
