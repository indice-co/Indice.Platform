import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ListViewType, MenuOption, ModalService, SearchOption } from '@indice/ng-components';
import { CasesApiService, CaseTypePartialResultSet, CheckpointType, } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { GeneralCasesComponent } from '../general-cases/general-cases.component';
import { CaseTypeService } from 'src/app/core/services/case-type.service';

@Component({
  selector: 'app-case-type-specific-cases-component',
  templateUrl: '../general-cases/general-cases.component.html'
})
export class CaseTypeSpecificCasesComponent extends GeneralCasesComponent implements OnInit {

  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _api: CasesApiService,
    protected _filterCachingService: FilterCachingService,
    protected _modalService: ModalService,
    protected _caseTypeService: CaseTypeService
  ) {
    super(_route, _router, _api, _filterCachingService, _modalService);
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

  getOtherSearchOptions(caseTypes: CaseTypePartialResultSet): SearchOption[] | undefined {
    const code = this.getFilterCacheKey();
    const caseType = caseTypes?.items?.find(x => x.code == code);
    return JSON.parse(caseType?.gridFilterConfig!) || [];
  }

  getCaseTypeCheckpoints(checkpointTypes: CheckpointType[]) {
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
  getCaseTypeSearchOption(caseTypes: CaseTypePartialResultSet) {
    const caseTypeSearchOption: SearchOption = {
      field: 'caseTypeCodes',
      name: 'ΤΥΠΟΣ ΥΠΟΘΕΣΗΣ',
      dataType: 'array',
      options: [],
      multiTerm: true
    }
    const code = this.getFilterCacheKey();
    const caseType = caseTypes.items?.find(x => x.code == code);
    caseTypeSearchOption.options?.push({ value: caseType?.code, label: caseType?.title! })
    return caseTypeSearchOption;
  }

  getExtraMetadataFilters(caseTypes: CaseTypePartialResultSet | undefined): string[] | undefined {
    const code = this.getFilterCacheKey();
    const metadata: string[] = [];
    const caseType = caseTypes?.items?.find(x => x.code == code);
    if (caseType && caseType.gridFilterConfig) {
      const gridFilterConfig = JSON.parse(caseType.gridFilterConfig);
      const fields: string[] = gridFilterConfig.map((config: any) => config.field);
      for (const field of fields) {
        this.filters?.filter(f => f.member === field)?.forEach(f => metadata?.push(`metadata.${this.stringifyFilterClause(f)}`));
      }
    }
    return metadata;
  }

  getFilterCacheKey(): string {
    const code = this._route.snapshot.paramMap.get('caseTypeCode');
    return code!;
  }
}

