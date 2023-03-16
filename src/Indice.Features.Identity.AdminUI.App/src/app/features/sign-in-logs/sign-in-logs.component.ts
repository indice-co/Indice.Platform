import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { IdentityApiService, SignInLogEntryResultSet, SignInLogEntry } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';

@Component({
    selector: 'app-sign-in-logs',
    templateUrl: './sign-in-logs.component.html'
})
export class SignInLogsComponent implements OnInit {
    constructor(
        private _api: IdentityApiService,
        private _modalService: NgbModal
    ) { }

    @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('signInLogsList', { static: true }) public signInLogsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public actionsTemplate: TemplateRef<HTMLElement>;
    public count = 0;
    public rows: SignInLogEntry[] = [];
    public columns: TableColumn[] = [];
    public selectedLogEntry: SignInLogEntry;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'id', name: 'Id', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'createdAt', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this.signInLogsList.dateTimeTemplate, width: 200 },
            { prop: 'actionName', name: 'Action', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'applicationName', name: 'App Name', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'sessionId', name: 'Session Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'location', name: 'Location', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'subjectName', name: 'Subject', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'resourceId', name: 'Endpoint', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'signInType', name: 'Sign in Type', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate }
        ];
    }

    public getLogs(event: SearchEvent): void {
        this._api.getSignInLogs(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((logs: SignInLogEntryResultSet) => {
            this.count = logs.count;
            this.rows = logs.items;
        });
    }

    public showLogDetails(row: SignInLogEntry, content: any): void {
        this.selectedLogEntry = row;
        this._modalService.open(content, { size: 'xl' });
    }
}
