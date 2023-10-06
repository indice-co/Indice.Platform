import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, SignInLogEntry, SignInLogEntryResultSet } from 'src/app/core/services/identity-api.service';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { ActivatedRoute, Router } from '@angular/router';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { QueryParameters } from 'src/app/shared/components/list-view/models/query-parameters';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-user-sign-in-logs',
  templateUrl: './user-sign-in-logs.component.html'
})
export class UserSignInLogsComponent implements OnInit {

  @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;
  @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
  @ViewChild('statusTemplate', { static: true }) private _statusTemplate: TemplateRef<HTMLElement>;
  @ViewChild('signInLogsList', { static: true }) public signInLogsList: ListViewComponent;
  @ViewChild('actionsTemplate', { static: true }) public actionsTemplate: TemplateRef<HTMLElement>;

  constructor(
    private _api: IdentityApiService,
    private _router: Router,
    private _route: ActivatedRoute,
    private _modalService: NgbModal
  ) { }

  public columns: TableColumn[] = [];
  public rows: SignInLogEntry[] = [];
  public count = 0;
  public defaultPageSize = 15;
  public defaultPage = 1;
  public defaultSortField = 'createdAt';
  public defaultSortDirection = 'Desc';
  public selectedLogEntry: SignInLogEntry;
  public isLoading: boolean = true;

  private _paramsCleared = true;

  ngOnInit(): void {
    this.columns = [
        { prop: 'createdAt', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._actionsTemplate, width: 200 },
        { prop: 'actionName', name: 'Action', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
        { prop: 'applicationName', name: 'App Name', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate },
        { name: 'Status', draggable: false, canAutoResize: false, sortable: false, resizeable: false, cellTemplate: this._statusTemplate },
        { prop: 'sessionId', name: 'Session Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
        { prop: 'location', name: 'Location', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
        { prop: 'resourceId', name: 'Endpoint', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
        { prop: 'signInType', name: 'Sign in Type', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate }
    ];
  }

  public getUserSignInLogs(event: SearchEvent): void {
    this.isLoading = true;
    const userId = this._route.parent.snapshot.params.id;
    const page = event.page;
    const pageSize = event.pageSize;
    const sortField = event.sortField;
    const defaultSortField = `${this.defaultSortField}${this.defaultSortDirection == 'Desc' ? '-' : '+'}`;
    this._paramsCleared = page == this.defaultPage && pageSize == this.defaultPageSize && sortField == defaultSortField;
    this._api.getSignInLogs(page, pageSize, sortField, undefined, userId)
      .pipe(finalize(() => {
        this.isLoading = false;
      }))
      .subscribe((resources: SignInLogEntryResultSet) => {
          this.count = resources.count;
          this.rows = resources.items;
      });
  }

  public showLogDetails(row: SignInLogEntry, content: any): void {
    this.selectedLogEntry = row;
    this._modalService.open(content, { size: 'xl' });
  }

  public refresh(): void {
    if (!this._paramsCleared) {
      this._paramsCleared = true;
      const params = {};
      params[QueryParameters.PAGE] = undefined;
      params[QueryParameters.PAGE_SIZE] = undefined;
      params[QueryParameters.SORT_FIELD] = undefined;
      params[QueryParameters.SORT_DIRECTION] = undefined;
      this._router.navigate([], { relativeTo: this._route, queryParams: params });
    }
    else {
      const defaultSortField = `${this.defaultSortField}${this.defaultSortDirection == 'Desc' ? '-' : '+'}`;
      this.getUserSignInLogs(new SearchEvent(this.defaultPage, this.defaultPageSize, defaultSortField));
    }
  }

  public getRowClass(row: any) {
    return {
        'bg-warning': row.review
    }
  }
}
