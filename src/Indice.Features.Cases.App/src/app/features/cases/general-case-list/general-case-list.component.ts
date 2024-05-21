import { Component } from '@angular/core';
import { BaseCaseListComponent } from '../base-case-list/base-case-list.component';
import { ActivatedRoute, Router } from '@angular/router';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { ModalService, SearchOption } from '@indice/ng-components';
import { forkJoin } from 'rxjs';
import { take } from 'rxjs/operators';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-cases-component',
  templateUrl: '../base-case-list/base-case-list.html'
})
export class GeneralCaseListComponent extends BaseCaseListComponent {
  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _caseTypeService: CaseTypeService,
    protected _api: CasesApiService,
    protected _filterCachingService: FilterCachingService,
    protected _modalService: ModalService,
    protected _datePipe: DatePipe
  ) {
    super(_route, _router, _api, _filterCachingService, _modalService, _caseTypeService, _datePipe);
  }

  ngOnInit() {
    this.setupParams();
    this.setSearchOptions();
    this.loadFilterSettings();
    this.loadColumnSettings();
    this.fetchCaseTypesAvailableForCreation();
    super.ngOnInit();
  }

  private setSearchOptions() {
    forkJoin({
      caseTypes: this._caseTypeService.getCaseTypeMenuItems(),
      checkpointTypes: this._api.getDistinctCheckpointTypes()
    }).pipe(take(1)).subscribe(({ caseTypes, checkpointTypes }) => {
      const commonSearchOptions = this.getCommonSearchOptions();
      const specificSearchOptions = [];
      if (this.filterConfig.CaseTypeCodes) {
        const caseTypeSearchOption: SearchOption = {
          field: 'caseTypeCodes',
          name: 'ΤΥΠΟΣ ΥΠΟΘΕΣΗΣ',
          dataType: 'array',
          options: caseTypes.map(caseType => ({ value: caseType.code, label: caseType?.title! })),
          multiTerm: true
        }
        specificSearchOptions.push(caseTypeSearchOption);
      }
      if (this.filterConfig.CheckpointTypeCodes) {
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
      this.searchOptions = [...commonSearchOptions, ...specificSearchOptions];
    });
  }
}
