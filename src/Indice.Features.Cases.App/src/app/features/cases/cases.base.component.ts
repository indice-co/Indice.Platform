import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BaseListComponent, FilterClause, Icons, IResultSet, ListViewType, MenuOption, ModalService, Operators, RouterViewAction, SearchOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasePartialResultSet, CasesApiService, CaseTypePartialResultSet, } from 'src/app/core/services/cases-api.service';
import { ParamsService } from 'src/app/core/services/params.service';
import { QueriesModalComponent } from 'src/app/shared/components/query-modal/query-modal.component';

@Component({
  selector: 'app-cases',
  templateUrl: './cases.base.html'
})
export class CasesBase extends BaseListComponent<CasePartial> implements OnInit {
  public newItemLink = 'new-case';
  public formActions: ViewAction[] = [
    new RouterViewAction(Icons.EntryView, 'queries', 'rightpane', 'Οι αναζητήσεις μου', 'Οι αναζητήσεις μου'),
    new ViewAction('refresh', 'refresh', null, Icons.Refresh, 'Ανανέωση στοιχείων')
  ];
  public queryParamsHasFilter = false;
  public tableFilters = new TableFilters();
  public tableColumns = new TableColumns();

  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _api: CasesApiService,
    protected _paramsService: ParamsService,
    protected _modalService: ModalService,
    protected _caseTypeMenuItemService: CaseTypeService
  ) {
    super(_route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 10;
    this.sort = 'createdByWhen';
    this.sortdir = 'desc';
    this.sortOptions = [
      new MenuOption('Ημ. Υποβολής', 'createdByWhen')
    ];
  }

  public ngOnInit(): void {
    super.ngOnInit();
  }

  setupParams(): void {
    // Are we already on "/cases"? If yes, reset params and return
    if (this._router.url === '/cases') {
      this._paramsService.resetParams();
    }

    //TODO: commenting caching for now - use params here
    // this._route.queryParams.subscribe((params: Params) => {
    //   const storedParams = this._paramsService.getParams();
    //   if (storedParams) {
    //     this._router.navigate(['/cases'], { queryParams: storedParams });
    //   }
    // });

    // Are there any filters in queryParams?
    this._route.queryParams.subscribe((params: Params) => {
      this.queryParamsHasFilter = params['filter'] ? true : false;
    });
  }

  fetchCaseTypesAvailableForCreation(): void {
    // independent call to fetch the case Types that the user can select for Case Creation
    this._api.getCaseTypes(true)
      .pipe(take(1))
      .subscribe(
        (caseTypesForCaseCreation: CasePartialResultSet) => {
          if (caseTypesForCaseCreation.count !== 0) {
            this.formActions.unshift(
              new RouterViewAction(Icons.Add, this.newItemLink, 'rightpane', 'Υποβολή νέας υπόθεσης', 'Νέα υπόθεση')
            );
          }
        }
      );
  }

  loadFilterSettings(): void {
    if (settings.caseListFilters === '') return;
    const filters = settings.caseListFilters.split(',')

    this.tableFilters.ReferenceNumber = filters.some(filter => filter === "ReferenceNumber");
    this.tableFilters.CustomerId = filters.some(filter => filter === "CustomerId");
    this.tableFilters.CustomerName = filters.some(filter => filter === "CustomerName");
    this.tableFilters.TaxId = filters.some(filter => filter === "TaxId");
    this.tableFilters.GroupIds = filters.some(filter => filter === "GroupIds");
    this.tableFilters.DateRange = filters.some(filter => filter === "DateRange");
    this.tableFilters.CaseTypeCodes = filters.some(filter => filter === "CaseTypeCodes");
    this.tableFilters.CheckpointTypeCodes = filters.some(filter => filter === "CheckpointTypeCodes");
  }

  // check environment variables and
  // switch on or off the list of predefined columns
  loadColumnSettings(): void {
    if (settings.caseListColumns === '') return;
    const columns = settings.caseListColumns.split(',')

    this.tableColumns.ReferenceNumber = columns.some(column => column === "ReferenceNumber");
    this.tableColumns.CustomerId = columns.some(column => column === "CustomerId");
    this.tableColumns.CustomerName = columns.some(column => column === "CustomerName");
    this.tableColumns.TaxId = columns.some(column => column === "TaxId");
    this.tableColumns.GroupId = columns.some(column => column === "GroupId");
    this.tableColumns.CaseType = columns.some(column => column === "CaseType");
    this.tableColumns.CheckpointType = columns.some(column => column === "CheckpointType");
    this.tableColumns.AssignedTo = columns.some(column => column === "AssignedTo");
    this.tableColumns.SubmitDate = columns.some(column => column === "SubmitDate");
  }

  openQueryModal(): void {
    this._modalService.show(QueriesModalComponent, {
      backdrop: 'static',
      keyboard: false
    });
  }

  loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
    const filterObject = this.buildFilters();
    return this.getFilteredCases(filterObject);
  }

  private getFilteredCases(filterObject: any) {
    const response = this._api
      .getCases(
        filterObject.customerIds,
        filterObject.customerNames,
        filterObject.from ? new Date(filterObject.from) : undefined,
        filterObject.to ? new Date(filterObject.to) : undefined,
        filterObject.caseTypeCodes,
        filterObject.checkpointTypeCodes,
        filterObject.groupIds,
        filterObject.filterMetadata,
        filterObject.referenceNumbers,
        this.page,
        this.pageSize,
        this.sortdir === 'asc' ? this.sort! : this.sort + '-',
        this.search || undefined,
        undefined
      )
      .pipe(
        take(1),
        map((result: CasePartialResultSet) => (result as IResultSet<CasePartial>))
      );

    return response;
  }

  private buildFilters() {

    let customerIds: string[] = [];
    this.filters?.filter(f => f.member === 'customerId')?.forEach(f => customerIds.push(this.stringifyFilterClause(f)));
    let customerNames: string[] = [];
    this.filters?.filter(f => f.member === 'customerName')?.forEach(f => customerNames.push(this.stringifyFilterClause(f)));
    let referenceNumbers: string[] = [];
    this.filters?.filter(f => f.member === 'referenceNumber')?.forEach(f => referenceNumbers.push(this.stringifyFilterClause(f)));
    let groupIds: string[] = [];
    this.filters?.filter(f => f.member === 'groupIds')?.forEach(f => groupIds?.push(this.stringifyFilterClause(f)));
    let from = this.filters?.find(f => f.member === 'dateRange' && f.operator === Operators.GREATER_THAN_EQUAL.value as FilterClause.Op)?.value;
    let to = this.filters?.find(f => f.member === 'dateRange' && f.operator === Operators.LESS_THAN_EQUAL.value as FilterClause.Op)?.value;
    let caseTypeCodes: string[] = [];
    this.filters?.filter(f => f.member === 'caseTypeCodes')?.forEach(f => caseTypeCodes?.push(this.stringifyFilterClause(f)));
    let checkpointTypeCodes: string[] = [];
    this.filters?.filter(f => f.member === 'checkpointTypeCodes')?.forEach(f => checkpointTypeCodes?.push(this.stringifyFilterClause(f)));
    let filterMetadata: string[] = [];
    this.filters?.filter(f => f.member === 'TaxId')?.forEach(f => filterMetadata?.push(`metadata.${this.stringifyFilterClause(f)}`));

    this.buildExtraFilters(filterMetadata);

    this._paramsService.setParams({
      view: this.view,
      page: this.page,
      pagesize: this.pageSize,
      search: this.search,
      sort: this.sort,
      dir: this.sortdir,
      filter: this.stringifyFilters(this.filters)
    });

    const filterObject = {
      "customerIds": customerIds,
      "customerNames": customerNames,
      "referenceNumbers": referenceNumbers,
      "groupIds": groupIds,
      "from": from,
      "to": to,
      "caseTypeCodes": caseTypeCodes,
      "checkpointTypeCodes": checkpointTypeCodes,
      "filterMetadata": filterMetadata,
    }

    return filterObject;
  }

  private buildExtraFilters(filterMetadata: string[]) {
    const code = this.getCodeFromParams();

    if (code) {
      //assuming every case type is of type "menu item"
      this._caseTypeMenuItemService.getCaseType(code).pipe(
        // Filter out any undefined or null gridFilterConfig
        map(caseType => caseType.gridFilterConfig ? JSON.parse(caseType.gridFilterConfig) : []),
        // Extract the "field" values from each object in the array
        map(gridFilterConfig => gridFilterConfig.map((config: any) => config.field))
      ).subscribe((fields: string[]) => {
        //add every "field" as "filter"
        for (const field of fields) {
          this.filters?.filter(f => f.member === field)?.forEach(f => filterMetadata?.push(`metadata.${this.stringifyFilterClause(f)}`));
        }
      });
    }
  }

  // TODO: make this public in Indice.Angular
  private stringifyFilters(filters: FilterClause[] | undefined) {
    return filters?.map((f: FilterClause) => {
      if (f.dataType === 'datetime') {
        f.value = (new Date(f.value)).toISOString();
      }
      return f.toString();
    }).join(',');
  }

  private stringifyFilterClause(filter: FilterClause): string {
    return `${filter.member}::${filter.operator}::${filter.value}`;
  }

  getCodeFromParams() {
    const lastSegment = this._route.snapshot.url[this._route.snapshot.url.length - 1]?.path;
    return lastSegment;
  }
}

class TableFilters {
  ReferenceNumber: boolean = false;
  CustomerId: boolean = true;
  CustomerName: boolean = true;
  TaxId: boolean = true;
  GroupIds: boolean = true;
  DateRange: boolean = true;
  CaseTypeCodes: boolean = true;
  CheckpointTypeCodes: boolean = true;
}

class TableColumns {
  ReferenceNumber: boolean = false;
  CustomerId: boolean = true;
  CustomerName: boolean = true;
  TaxId: boolean = true;
  GroupId: boolean = true;
  CaseType: boolean = true;
  CheckpointType: boolean = true;
  AssignedTo: boolean = true;
  SubmitDate: boolean = true;
}
