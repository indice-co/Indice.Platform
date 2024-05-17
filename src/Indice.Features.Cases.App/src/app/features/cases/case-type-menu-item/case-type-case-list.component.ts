import { Component } from '@angular/core';
import { BaseCaseListComponent } from '../base-case-list.component';
import { ActivatedRoute, NavigationEnd, ParamMap, Router } from '@angular/router';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { ModalService, SearchOption } from '@indice/ng-components';
import { switchMap, take } from 'rxjs/operators';
import { forkJoin, Subscription } from 'rxjs';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-cases-type-menu-item-component',
  templateUrl: '../base-case-list.html'
})
export class CaseTypeCaseListComponent extends BaseCaseListComponent {
  private routerSubscription: Subscription = new Subscription();

  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _caseTypeService: CaseTypeService,
    protected _api: CasesApiService,
    protected _filterCachingService: FilterCachingService,
    protected _modalService: ModalService,
    protected _datePipe: DatePipe,
  ) {
    super(_route, _router, _api, _filterCachingService, _modalService, _caseTypeService, _datePipe);
  }

  ngOnInit() {
    super.ngOnInit();
    this.setSearchOptions();
    this.loadFilterSettings();
    this.loadColumnSettings();
    this.fetchCaseTypesAvailableForCreation();
    this.setColumnsFromConfig()

    //TODO: change if to also make this to only apply to "/cases"
    this.routerSubscription = this._router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.setupParams();
        this.setColumnsFromConfig()
        this.setSearchOptions();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe(); // Unsubscribe from router events to prevent memory leaks
    }
  }

  private setSearchOptions() {
    this._route.paramMap.pipe(
      switchMap((params: ParamMap) => {
        const caseTypeCode = params.get('caseTypeCode') ?? "";
        return forkJoin({
          caseType: this._caseTypeService.getCaseType(caseTypeCode),
          checkpointTypes: this._api.getDistinctCheckpointTypes(),
        });
      }),
      take(1)
    ).subscribe(({ caseType, checkpointTypes }) => {
      const commonSearchOptions = this.getCommonSearchOptions();
      const specificSearchOptions: SearchOption[] = [];
      if (this.tableFilters.CaseTypeCodes) {
        const caseTypeSearchOption: SearchOption = {
          field: 'caseTypeCodes',
          name: 'ΤΥΠΟΣ ΥΠΟΘΕΣΗΣ',
          dataType: 'array',
          options: [{ value: caseType?.code, label: caseType?.title! }],
          multiTerm: true
        }
        specificSearchOptions.push(caseTypeSearchOption);
      }
      if (this.tableFilters.CheckpointTypeCodes) {
        const checkpointTypeSearchOption: SearchOption = {
          field: 'checkpointTypeCodes',
          name: 'ΤΡΕΧΟΝ ΣΗΜΕΙΟ ΕΛΕΓΧΟΥ',
          dataType: 'array',
          options: checkpointTypes.map(checkpointType => ({
            value: checkpointType?.code,
            label: checkpointType?.title ?? checkpointType?.code!
          })),
          multiTerm: true
        };
        specificSearchOptions.push(checkpointTypeSearchOption);
      }
      //add filters from the case type configuration
      specificSearchOptions.push(...JSON.parse(caseType?.gridFilterConfig!) || [])
      this.searchOptions = [...commonSearchOptions, ...specificSearchOptions];
    });
  }

  private setColumnsFromConfig() {
    this._caseTypeService.getCaseType(this.getCodeFromParams()).subscribe(caseType => {
      const columnArray = JSON.parse(caseType?.gridColumnConfig!) || [];
      for (const item of columnArray) {
        this.columns.push(item)
      }
    });
  }
}
