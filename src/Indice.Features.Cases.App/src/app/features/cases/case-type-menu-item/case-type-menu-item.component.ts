import { Component } from '@angular/core';
import { CasesBase } from '../cases.base.component';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { ModalService, SearchOption } from '@indice/ng-components';
import { take } from 'rxjs/operators';
import { forkJoin, Subscription } from 'rxjs';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-cases-type-menu-item-component',
  templateUrl: '../cases.base.html'
})
export class CaseTypeMenuItemComponent extends CasesBase {
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
    this.loadFilterSettings();
    this.loadColumnSettings();
    this.addSearchOptions();
    this.fetchCaseTypesAvailableForCreation();
    //TODO: change if to also make this to only apply to "/cases"
    this.routerSubscription = this._router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.setupParams();
        this.addSearchOptions();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe(); // Unsubscribe from router events to prevent memory leaks
    }
  }


  private addSearchOptions() {
    forkJoin({
      caseType: this._caseTypeService.getCaseType(this.getCodeFromParams()),
      checkpointTypes: this._api.getDistinctCheckpointTypes(),
    }).pipe(take(1)).subscribe(({ caseType, checkpointTypes }) => {
      if (this.tableFilters.CaseTypeCodes) {
        const caseTypeSearchOption: SearchOption = {
          field: 'caseTypeCodes',
          name: 'ΤΥΠΟΣ ΥΠΟΘΕΣΗΣ',
          dataType: 'array',
          options: [{ value: caseType?.code, label: caseType?.title! }],
          multiTerm: true
        }
        this.searchOptions.push(caseTypeSearchOption);
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
        this.searchOptions.push(checkpointTypeSearchOption);
      }
      //add filters from the case type configuration
      const filtersArray = JSON.parse(caseType?.gridFilterConfig!) || [];
      for (const item of filtersArray) {
        this.searchOptions.push(item)
      }
      const columnArray = JSON.parse(caseType?.gridColumnConfig!) || [];
      for (const item of columnArray) {
        this.columns.push(item)
      }
      // now that we have the searchOptions, call parent's ngOnInit!
      super.ngOnInit();
    });
  }
}
