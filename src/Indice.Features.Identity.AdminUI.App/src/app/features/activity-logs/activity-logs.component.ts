import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { CellContext, TableColumn } from '@swimlane/ngx-datatable';
import { NgbDateStruct, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { IdentityApiService, ActivityLogEntryResultSet, ActivityLogEntry } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { NgbDateCustomParserFormatter } from 'src/app/shared/services/custom-parser-formatter.service';
import { QueryParameters } from 'src/app/shared/components/list-view/models/query-parameters';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

@Component({
    selector: 'app-activity-logs',
    templateUrl: './activity-logs.component.html',
    providers: [NgbDateCustomParserFormatter],
    standalone: false
})
export class ActivityLogsComponent implements OnInit {
    constructor(
        private _api: IdentityApiService,
        private _modalService: NgbModal,
        private _dateParser: NgbDateCustomParserFormatter,
        private _router: Router,
        private _route: ActivatedRoute
    ) { }

    @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('statusTemplate', { static: true }) private _statusTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('ActivityLogsList', { static: true }) public ActivityLogsList: ListViewComponent;
    @ViewChild('ipCellTemplate', { static: true }) private _ipCellTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('eventTypeCellTemplate', { static: true }) private _eventTypeCellTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('subjectNameTemplate', { static: true }) private _subjectNameTemplate: TemplateRef<CellContext<any>>;

    public count = 0;
    public rows: ActivityLogEntry[] = [];
    public columns: TableColumn[] = [];
    public selectedLogEntry: ActivityLogEntry;
    public defaultPage: number = 1;
    public defaultPageSize: number = 15;
    public defaultSortField: string = 'createdAt';
    public defaultSortDirection: string = 'Desc';
    public isLoading = true;
    public filter = {
        dateFrom: undefined,
        dateTo: undefined,
        succeeded: undefined,
        subject: undefined,
        actionName: undefined
    }
    public objectKeys = Object.keys;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'createdAt', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._actionsTemplate, width: 200 },
            { prop: 'actionName', name: 'Action', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._eventTypeCellTemplate },
            { prop: 'category', name: 'Category', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'applicationName', name: 'App Name', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'subjectName', name: 'Subject', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._subjectNameTemplate },
            { prop: 'resourceType', name: 'Resource Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
            { prop: 'ipAddress', name: 'IP Address', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._ipCellTemplate },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate },
        ];
    }

    public getLogs(event: SearchEvent): void {
        let dateFrom = event.filter.dateFrom ? (new Date(event.filter.dateFrom)) : undefined;
        let dateTo = event.filter.dateFrom ? (new Date(event.filter.dateTo)) : undefined;
        this._api.getActivityLogs(
            event.page,
            event.pageSize,
            event.sortField,
            event.searchTerm,
            event.filter.subject,
            undefined /*sessionId*/,
            undefined /*markedForReview*/,
            event.filter.succeeded,
            event.filter.actionName,
            dateFrom,
            dateTo,
            undefined  /*applicationId*/)
            .pipe(finalize(() => {
                this.isLoading = false;
            }))
            .subscribe((logs: ActivityLogEntryResultSet) => {
                this.count = logs.count;
                this.rows = logs.items;
                this.filter.succeeded = event.filter.succeeded ? event.filter.succeeded == "true" ? true : false : undefined;
                this.filter.dateFrom = event.filter.dateFrom ? this._dateParser.parseDate(new Date(event.filter.dateFrom)) : undefined;
                this.filter.dateTo = event.filter.dateTo ? this._dateParser.parseDate(new Date(event.filter.dateTo)) : undefined;
                this.filter.subject = event.filter.subject;
                this.filter.actionName = event.filter.actionName;
            });
    }

    public showLogDetails(row: ActivityLogEntry, content: any): void {
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
        if (this.filter.actionName) {
            params['actionName'] = this.filter.actionName
        }
        this._router.navigate([], { relativeTo: this._route, queryParams: params });
    }
}
