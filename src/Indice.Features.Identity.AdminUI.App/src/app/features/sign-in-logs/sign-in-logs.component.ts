import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbDateStruct, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { IdentityApiService, SignInLogEntryResultSet, SignInLogEntry } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { NgbDateCustomParserFormatter } from 'src/app/shared/services/custom-parser-formatter.service';
import { QueryParameters } from 'src/app/shared/components/list-view/models/query-parameters';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

@Component({
    selector: 'app-sign-in-logs',
    templateUrl: './sign-in-logs.component.html',
    providers: [NgbDateCustomParserFormatter]
})
export class SignInLogsComponent implements OnInit {
    constructor(
        private _api: IdentityApiService,
        private _modalService: NgbModal,
        private _dateParser: NgbDateCustomParserFormatter,
        private _router: Router,
        private _route: ActivatedRoute
    ) { }

    @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;
    @ViewChild('statusTemplate', { static: true }) private _statusTemplate: TemplateRef<HTMLElement>;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('signInLogsList', { static: true }) public signInLogsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) public actionsTemplate: TemplateRef<HTMLElement>;
    public count = 0;
    public rows: SignInLogEntry[] = [];
    public columns: TableColumn[] = [];
    public selectedLogEntry: SignInLogEntry;
    public defaultPage: number = 1;
    public defaultPageSize: number = 15;
    public defaultSortField: string = 'createdAt';
    public defaultSortDirection: string = 'Desc';
    public isLoading = true;
    public filter = {
        dateFrom: undefined,
        dateTo: undefined,
        succeeded: undefined,
        subject: undefined
    }

    public ngOnInit(): void {
        this.columns = [
            { prop: 'id', name: 'Id', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'createdAt', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this.signInLogsList.dateTimeTemplate, width: 200 },
            { prop: 'actionName', name: 'Action', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'applicationName', name: 'App Name', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate },
            { name: 'Status', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._statusTemplate },
            { prop: 'sessionId', name: 'Session Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'location', name: 'Location', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'subjectName', name: 'Subject', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'resourceId', name: 'Endpoint', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'signInType', name: 'Sign in Type', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate }
        ];
    }

    public getLogs(event: SearchEvent): void {
        let dateFrom = event.filter.dateFrom ? (new Date(event.filter.dateFrom)).toISOString() : undefined;
        let dateTo = event.filter.dateFrom ? (new Date(event.filter.dateTo)).toISOString() : undefined;
        this._api.getSignInLogs(event.page , event.pageSize, event.sortField, event.searchTerm, event.filter.subject, undefined, undefined, event.filter.succeeded, dateFrom, dateTo)
            .pipe(finalize(() => {
                this.isLoading = false;
            }))
            .subscribe((logs: SignInLogEntryResultSet) => {
                this.count = logs.count;
                this.rows = logs.items;
                this.filter.succeeded = event.filter.succeeded ? event.filter.succeeded == "true" ? true : false : undefined;
                this.filter.dateFrom = event.filter.dateFrom ? this._dateParser.parseDate(new Date(event.filter.dateFrom)) : undefined;
                this.filter.dateTo = event.filter.dateTo ? this._dateParser.parseDate(new Date(event.filter.dateTo)) : undefined;
                this.filter.subject = event.filter.subject;
            });
    }

    public showLogDetails(row: SignInLogEntry, content: any): void {
        this.selectedLogEntry = row;
        this._modalService.open(content, { size: 'xl' });
    }

    public search() {
        const params = {};
        params[QueryParameters.PAGE] = this.defaultPage;
        params[QueryParameters.PAGE_SIZE] = this.defaultPageSize;
        params[QueryParameters.SORT_FIELD] = this.defaultSortField;
        params[QueryParameters.SORT_DIRECTION] = this.defaultSortDirection;
        if (this.filter.dateFrom) {
            params['dateFrom'] = this._dateParser.format(this.filter.dateFrom as NgbDateStruct)
        }
        if (this.filter.dateTo) {
            params['dateTo'] = this._dateParser.format(this.filter.dateTo as NgbDateStruct)
        }
        if (this.filter.succeeded !== undefined) {
            params['succeeded'] = this.filter.succeeded
        }
        if (this.filter.subject) {
            params['subject'] = this.filter.subject
        }
        this._router.navigate([], { relativeTo: this._route, queryParams: params });
    }
}
