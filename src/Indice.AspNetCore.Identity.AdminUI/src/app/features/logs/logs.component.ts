import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, LogInfoResultSet, LogInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';

@Component({
    selector: 'app-logs',
    templateUrl: './logs.component.html'
})
export class LogsComponent implements OnInit {
    @ViewChild('logsList', { static: true }) public _logsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public _actionsTemplate: TemplateRef<HTMLElement>;

    constructor(private _api: IdentityApiService, private _modalService: NgbModal) { }

    public count = 0;
    public rows: LogInfo[] = [];
    public columns: TableColumn[] = [];
    public selectedLog: LogInfo;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'level', name: 'Level', draggable: false, canAutoResize: false, sortable: false, resizeable: false, width: 115, cellClass: this.getLevelClass },
            { prop: 'message', name: 'Message', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'userName', name: 'Username', draggable: false, canAutoResize: false, sortable: false, resizeable: false, width: 250 },
            { prop: 'timestamp', name: 'Timestamp', draggable: false, canAutoResize: false, sortable: false, resizeable: false, width: 250, cellTemplate: this._logsList.dateTimeTemplate },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
    }

    public getLogs(event: SearchEvent): void {
        this._api.getLogs(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((logs: LogInfoResultSet) => {
            this.count = logs.count;
            this.rows = logs.items;
        });
    }

    public getLevelClass(data: any): string {
        if (data.row.level === 'Information') {
            return ' log-information';
        }
        if (data.row.level === 'Warning') {
            return ' log-warning';
        }
        if (data.row.level === 'Error') {
            return ' log-error';
        }
        return '';
    }

    public openEventDetails(log: LogInfo, content: any): void {
        this.selectedLog = log;
        this._modalService.open(content);
    }
}
