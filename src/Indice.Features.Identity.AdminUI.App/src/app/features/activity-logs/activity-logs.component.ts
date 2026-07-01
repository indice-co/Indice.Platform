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
    @ViewChild('deviceTemplate', { static: true }) private _deviceTemplate: TemplateRef<CellContext<any>>;

    public count = 0;
    public rows: ActivityLogEntry[] = [];
    public columns: TableColumn[] = [];
    public selectedLogEntry: ActivityLogEntry;
    public defaultPage: number = 1;
    public defaultPageSize: number = 15;
    public defaultSortField: string = 'createdAt';
    public defaultSortDirection: string = 'desc';
    // Taller than the default 50px so wrapped descriptions (~2 lines) stay fully visible instead of being clipped.
    public rowHeight: number = 64;
    public isLoading = true;
    public filter = {
        dateFrom: undefined,
        dateTo: undefined,
        succeeded: undefined,
        subject: undefined,
        actionName: undefined,
        resourceId: undefined,
        resourceType: undefined,
        category: undefined
    }
    public objectKeys = Object.keys;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'createdAt', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._actionsTemplate, width: 170 },
            { prop: 'subjectName', name: 'Subject', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._subjectNameTemplate, width: 200 },
            { prop: 'actionName', name: 'Action', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._eventTypeCellTemplate, width: 220 },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate, width: 360 },
            { prop: 'category', name: 'Category', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate, width: 150 },
            { prop: 'resourceType', name: 'Resource Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate, width: 160 },
            { prop: 'resourceId', name: 'Resource Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate, width: 280 },
            { prop: 'applicationName', name: 'App Name', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate, width: 170 },
            { prop: 'ipAddress', name: 'IP Address', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._ipCellTemplate, width: 170 },
            { prop: 'extraData.device.displayName', name: 'Device', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._deviceTemplate, width: 200 },
        ];
    }

    public getLogs(event: SearchEvent): void {
        const dateFrom = event.filter.dateFrom ? new Date(event.filter.dateFrom) : undefined;
        const dateTo = event.filter.dateTo ? new Date(event.filter.dateTo) : undefined;
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
            event.filter.resourceId,
            event.filter.resourceType,
            event.filter.category,
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
                this.filter.resourceId = event.filter.resourceId;
                this.filter.resourceType = event.filter.resourceType;
                this.filter.category = event.filter.category;
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
        if (this.filter.resourceId) {
            params['resourceId'] = this.filter.resourceId
        }
        if (this.filter.resourceType) {
            params['resourceType'] = this.filter.resourceType
        }
        if (this.filter.category) {
            params['category'] = this.filter.category
        }
        this._router.navigate([], { relativeTo: this._route, queryParams: params });
    }
}
