import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BaseListComponent, FilterClause, Icons, IResultSet, ListViewType, MenuOption, ModalService, Operators, RouterViewAction, SearchOption, ViewAction } from '@indice/ng-components';
import { forkJoin, Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasePartialResultSet, CasesApiService, CaseTypePartialResultSet, CheckpointType, } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { QueriesModalComponent } from 'src/app/shared/components/query-modal/query-modal.component';

@Component({
  selector: 'app-general-cases-component',
  templateUrl: './general-cases.component.html'
})
export class GeneralCasesComponent extends BaseListComponent<CasePartial> implements OnInit {
  public newItemLink = 'new-case';
  public formActions: ViewAction[] = [
    new RouterViewAction(Icons.EntryView, 'queries', 'rightpane', 'Οι αναζητήσεις μου', 'Οι αναζητήσεις μου'),
    new ViewAction('refresh', 'refresh', null, Icons.Refresh, 'Ανανέωση στοιχείων')
  ];
  public queryParamsHasFilter = false;
  public tableFilters = new TableFilters();
  public tableColumns = new TableColumns();
  protected caseTypes: CaseTypePartialResultSet | undefined;

  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _api: CasesApiService,
    protected _filterCachingService: FilterCachingService,
    protected _modalService: ModalService,
    protected _caseTypeService: CaseTypeService
  ) {
    super(_route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 10;
    this.sort = 'createdByWhen';
    this.sortdir = 'desc';
    this.sortOptions = [
      new MenuOption('Ημ. Υποβολής', 'createdByWhen')
    ];
    this.loadFilterSettings();
    this.loadColumnSettings();
  }

  public ngOnInit(): void {
    this.initialize();
    this.createNewCaseButton();
  }

  public initialize(): void {
    const key = this.getFilterCacheKey();
    const storedParams = this._filterCachingService.getParams(key);
    if (storedParams) {
      this._router.navigate([], {
        relativeTo: this._route,
        queryParams: storedParams
      });
    }
    // Are there any filters in queryParams?
    this._route.queryParams.subscribe((params: Params) => {
      this.queryParamsHasFilter = params['filter'] ? true : false;
    });
    forkJoin({
      caseTypes: this._api.getCaseTypes(),
      checkpointTypes: this._caseTypeService.getDistinctCheckpointTypes()
    }).pipe(take(1)).subscribe(({ caseTypes, checkpointTypes }) => {
      //todo this should not be needed - we assign this so its available for the async calls
      this.caseTypes = caseTypes;
      const tempSearchOptions: SearchOption[] = [];
      if (this.tableFilters.CustomerId) {
        tempSearchOptions.push({
          field: 'referenceNumber',
          name: 'ΑΡΙΘΜΟΣ ΥΠΟΘΕΣΗΣ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.CustomerId) {
        tempSearchOptions.push({
          field: 'customerId',
          name: 'ΚΩΔΙΚΟΣ ΠΕΛΑΤΗ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.CustomerName) {
        tempSearchOptions.push({
          field: 'customerName',
          name: 'ΟΝΟΜΑ ΠΕΛΑΤΗ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.TaxId) {
        tempSearchOptions.push({
          field: 'TaxId', // this must be exactly the same "case-wise" with db's json property!
          name: 'Α.Φ.Μ. ΠΕΛΑΤΗ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.GroupIds) {
        tempSearchOptions.push({
          field: 'groupIds',
          name: 'ΑΡΙΘΜΟΣ ΚΑΤΑΣΤΗΜΑΤΟΣ',
          dataType: 'string',
          multiTerm: true
        });
      }
      if (this.tableFilters.DateRange) {
        tempSearchOptions.push({
          field: 'dateRange',
          name: 'ΗΜ. ΥΠΟΒΟΛΗΣ',
          dataType: 'daterange'
        });
      }
      if (this.tableFilters.CaseTypeCodes) {
        const caseTypeSearchOption = this.getCaseTypeSearchOption(caseTypes);
        tempSearchOptions.push(caseTypeSearchOption);
      }
      if (this.tableFilters.CheckpointTypeCodes) {
        const checkpointTypeSearchOption = this.getCaseTypeCheckpoints(checkpointTypes);
        tempSearchOptions.push(checkpointTypeSearchOption);
      }
      const otherSearchOptions = this.getOtherSearchOptions(caseTypes);
      if (otherSearchOptions) {
        tempSearchOptions.push(...otherSearchOptions);
      }
      this.searchOptions = tempSearchOptions;
      // now that we have the searchOptions, call parent's ngOnInit!
      super.ngOnInit();
    });

  }

  public loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
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
    const extraMetadataFilters = this.getExtraMetadataFilters(this.caseTypes);
    if (extraMetadataFilters) {
      filterMetadata?.push(...extraMetadataFilters)
    }
    this._filterCachingService.setParams(this.getFilterCacheKey(), {
      view: this.view,
      page: this.page,
      pagesize: this.pageSize,
      search: this.search,
      sort: this.sort,
      dir: this.sortdir,
      filter: this.stringifyFilters(this.filters)
    });
    return this._api
      .getCases(
        customerIds,
        customerNames,
        from ? new Date(from) : undefined,
        to ? new Date(to) : undefined,
        caseTypeCodes,
        checkpointTypeCodes,
        groupIds,
        filterMetadata,
        referenceNumbers,
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
  }

  private createNewCaseButton(): void {
    // independent call to fetch the case Types that the user can select for Case Creation
    this._caseTypeService.getCanCreateCaseTypes()
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

  public openQueryModal(): void {
    this._modalService.show(QueriesModalComponent, {
      backdrop: 'static',
      keyboard: false
    });
  }

  protected getOtherSearchOptions(caseTypes: CaseTypePartialResultSet): SearchOption[] | undefined {
    return undefined;
  }

  protected getCaseTypeCheckpoints(checkpointTypes: CheckpointType[]) {
    const checkpointTypeSearchOption: SearchOption = {
      field: 'checkpointTypeCodes',
      name: 'ΤΡΕΧΟΝ ΣΗΜΕΙΟ ΕΛΕΓΧΟΥ',
      dataType: 'array',
      options: [],
      multiTerm: true
    }
    for (let checkpointType of checkpointTypes) { // fill checkpointTypeSearchOption's SelectInputOptions
      checkpointTypeSearchOption.options?.push({ value: checkpointType?.code, label: checkpointType?.title ?? checkpointType?.code! })
    }
    return checkpointTypeSearchOption;
  }

  //add all case types to search options
  protected getCaseTypeSearchOption(caseTypes: CaseTypePartialResultSet) {
    const caseTypeSearchOption: SearchOption = {
      field: 'caseTypeCodes',
      name: 'ΤΥΠΟΣ ΥΠΟΘΕΣΗΣ',
      dataType: 'array',
      options: [],
      multiTerm: true
    }
    for (let caseType of caseTypes.items!) { // fill caseTypeSearchOption's SelectInputOptions
      caseTypeSearchOption.options?.push({ value: caseType.code, label: caseType?.title! })
    }
    return caseTypeSearchOption;
  }

  protected getFilterCacheKey(): string {
    return "cases";
  }

  protected getExtraMetadataFilters(caseTypes: CaseTypePartialResultSet | undefined): string[] | undefined {
    return undefined;
  }

  private loadFilterSettings(): void {
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

  private loadColumnSettings(): void {
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

  // TODO: make this public in Indice.Angular
  private stringifyFilters(filters: FilterClause[] | undefined) {
    return filters?.map((f: FilterClause) => {
      if (f.dataType === 'datetime') {
        f.value = (new Date(f.value)).toISOString();
      }
      return f.toString();
    }).join(',');
  }

  public stringifyFilterClause(filter: FilterClause): string {
    return `${filter.member}::${filter.operator}::${filter.value}`;
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
