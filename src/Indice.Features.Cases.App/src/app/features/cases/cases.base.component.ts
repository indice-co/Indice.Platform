import { DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BaseListComponent, FilterClause, Icons, IResultSet, ListViewType, MenuOption, ModalService, Operators, RouterViewAction, SearchOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { settings } from 'src/app/core/models/settings';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasePartial, CasePartialResultSet, CasesApiService, CaseTypePartial, } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
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

  public columns = [
    { key: 'ReferenceNumber' },
    { key: 'CustomerId' },
    { key: 'CustomerName' },
    { key: 'TaxId', itemProperty: 'metadata.TaxId' },
    { key: 'GroupId' },
    { key: 'CaseType', itemProperty: 'caseType.title' },
    { key: 'CheckpointType', itemProperty: 'checkpointType.title' },
    { key: 'AssignedTo', itemProperty: 'assignedToName' },
    { key: 'SubmitDate', itemProperty: 'createdByWhen' }
  ];

  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _api: CasesApiService,
    protected _filterCachingService: FilterCachingService,
    protected _modalService: ModalService,
    protected _caseTypeService: CaseTypeService,
    protected datePipe: DatePipe,
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
    this.setupParams();
  }

  public getItemValue(item: any, column: any) {
    const value = this.findAccordingValue(item, column);
    let formattedValue = value;
    if (value instanceof Date) {
      // Format date using DatePipe
      formattedValue = this.datePipe.transform(value, 'dd/MM/yy, HH:mm') || '-';
    }
    if (value === undefined || value === null) {
      formattedValue = '-';
    }
    return formattedValue;
  }

  private findAccordingValue(item: any, column: any): any {
    if (column.itemProperty) {
      return this.getValueFromProperty(item, column.itemProperty)
    }
    const itemProperty = column.key;
    const formattedProperty = itemProperty[0].toLowerCase() + itemProperty.slice(1);
    return item[formattedProperty];
  }

  private getValueFromProperty(obj: any, propPath: any) {
    const props = propPath.split('.');
    for (const prop of props) {
      obj = obj[prop];
      if (obj === undefined) {
        return obj;
      }
    }
    return obj;
  }

  public setupParams(): void {
    const code = this.getCodeFromParams();
    //TODO why do we have this?
    //Are we already on "/cases"? If yes, reset params and return
    // if (this._router.url === '/cases') {
    //   this._filterCachingService.resetParams(code ?? "cases");
    // }
    const storedParams = this._filterCachingService.getParams(code ?? "cases");
    if (storedParams) {
      //Get current url then add queryparams
      this._router.navigate([], {
        relativeTo: this._route,
        queryParams: storedParams
      });
    }
    this._route.queryParams.subscribe((params: Params) => {
      // Are there any filters in queryParams?
      this.queryParamsHasFilter = params['filter'] ? true : false;
    });
  }

  public fetchCaseTypesAvailableForCreation(): void {
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

  public loadFilterSettings(): void {
    if (settings.caseListFilters === '') return;
    const filters = settings.caseListFilters.split(',');
    for (const key of Object.keys(this.tableFilters)) {
      this.tableFilters[key as keyof TableFilters] = filters.includes(key);
    }
  }

  // check environment variables and switch on/off the list of predefined columns
  public loadColumnSettings(): void {
    const defaultColumns = ["CustomerId", "CustomerName", "TaxId", "GroupId", "CaseType", "CheckpointType", "AssignedTo", "SubmitDate"];
    const configColumns = settings.caseListColumns === '' ? defaultColumns : settings.caseListColumns.split(',');
    for (const column of this.columns) {
      this.tableColumns[column.key] = configColumns.includes(column.key);
    }
  }

  public openQueryModal(): void {
    this._modalService.show(QueriesModalComponent, {
      backdrop: 'static',
      keyboard: false
    });
  }

  public loadItems(): Observable<IResultSet<CasePartial> | null | undefined> {
    const filterObject = this.buildFilters();
    return this.getFilteredCases(filterObject);
  }

  public getCodeFromParams() {
    const code = this._route.snapshot.paramMap.get('caseTypeCode');
    return code!;
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
    const code = this.getCodeFromParams();
    if (code) {
      this.buildExtraFilters(filterMetadata, code);
    }
    const filterParams = {
      view: this.view,
      page: this.page,
      pagesize: this.pageSize,
      search: this.search,
      sort: this.sort,
      dir: this.sortdir,
      filter: this.stringifyFilters(this.filters)
    };
    this._filterCachingService.setParams(code ?? "cases", filterParams);
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

  private buildExtraFilters(filterMetadata: string[], code: string) {
    this._caseTypeService.getCaseType(code).subscribe((caseType: CaseTypePartial | undefined) => {
      if (caseType && caseType.gridFilterConfig) {
        const gridFilterConfig = JSON.parse(caseType.gridFilterConfig);
        const fields: string[] = gridFilterConfig.map((config: any) => config.field);
        for (const field of fields) {
          this.filters?.filter(f => f.member === field)?.forEach(f => filterMetadata?.push(`metadata.${this.stringifyFilterClause(f)}`));
        }
      }
    });
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

  protected getCommonSearchOptions(): SearchOption[] {
    const searchOptions: SearchOption[] = [];

    if (this.tableFilters.CustomerId) {
      searchOptions.push({ field: 'referenceNumber', name: 'ΑΡΙΘΜΟΣ ΥΠΟΘΕΣΗΣ', dataType: 'string' });
      searchOptions.push({ field: 'customerId', name: 'ΚΩΔΙΚΟΣ ΠΕΛΑΤΗ', dataType: 'string' });
    }

    if (this.tableFilters.CustomerName) {
      searchOptions.push({ field: 'customerName', name: 'ΟΝΟΜΑ ΠΕΛΑΤΗ', dataType: 'string' });
    }

    if (this.tableFilters.TaxId) {
      searchOptions.push({ field: 'TaxId', name: 'Α.Φ.Μ. ΠΕΛΑΤΗ', dataType: 'string' });
    }

    if (this.tableFilters.GroupIds) {
      searchOptions.push({ field: 'groupIds', name: 'ΑΡΙΘΜΟΣ ΚΑΤΑΣΤΗΜΑΤΟΣ', dataType: 'string', multiTerm: true });
    }

    if (this.tableFilters.DateRange) {
      searchOptions.push({ field: 'dateRange', name: 'ΗΜ. ΥΠΟΒΟΛΗΣ', dataType: 'daterange' });
    }

    return searchOptions;
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
  [key: string]: boolean;
}
