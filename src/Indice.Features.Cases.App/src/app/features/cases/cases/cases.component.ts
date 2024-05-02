import { Component } from '@angular/core';
import { CasesBase } from '../cases.base.component';
import { ActivatedRoute, Router } from '@angular/router';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartialResultSet } from 'src/app/core/services/cases-api.service';
import { ParamsService } from 'src/app/core/services/params.service';
import { ModalService, SearchOption } from '@indice/ng-components';
import { forkJoin, Observable } from 'rxjs';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-cases-component',
  templateUrl: '../cases.base.html'
})
export class CasesComponent extends CasesBase {
  constructor(
    protected _route: ActivatedRoute,
    protected _router: Router,
    protected _caseTypeMenuItemService: CaseTypeService,
    protected _api: CasesApiService,
    protected _paramsService: ParamsService,
    protected _modalService: ModalService
  ) {
    super(_route, _router, _api, _paramsService, _modalService, _caseTypeMenuItemService);
  }

  ngOnInit() {
    super.ngOnInit();
    this.loadFilterSettings();
    this.loadColumnSettings();
    this.setupParams();
    this.initializeSearchOptions();
    this.fetchCaseTypesAvailableForCreation();
  }

  initializeSearchOptions() {
    forkJoin({
      caseTypes: this._caseTypeMenuItemService.getCaseTypeMenuItems(),
      checkpointTypes: this._api.getDistinctCheckpointTypes()
    }).pipe(take(1)).subscribe(({ caseTypes, checkpointTypes }) => {
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

      this.searchOptions = [];
      if (this.tableFilters.CustomerId) {
        this.searchOptions.push({
          field: 'referenceNumber',
          name: 'ΑΡΙΘΜΟΣ ΥΠΟΘΕΣΗΣ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.CustomerId) {
        this.searchOptions.push({
          field: 'customerId',
          name: 'ΚΩΔΙΚΟΣ ΠΕΛΑΤΗ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.CustomerName) {
        this.searchOptions.push({
          field: 'customerName',
          name: 'ΟΝΟΜΑ ΠΕΛΑΤΗ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.TaxId) {
        this.searchOptions.push({
          field: 'TaxId', // this must be exactly the same "case-wise" with db's json property!
          name: 'Α.Φ.Μ. ΠΕΛΑΤΗ',
          dataType: 'string'
        });
      }
      if (this.tableFilters.GroupIds) {
        this.searchOptions.push({
          field: 'groupIds',
          name: 'ΑΡΙΘΜΟΣ ΚΑΤΑΣΤΗΜΑΤΟΣ',
          dataType: 'string',
          multiTerm: true
        });
      }
      if (this.tableFilters.DateRange) {
        this.searchOptions.push({
          field: 'dateRange',
          name: 'ΗΜ. ΥΠΟΒΟΛΗΣ',
          dataType: 'daterange'
        });
      }
      if (this.tableFilters.CaseTypeCodes) {
        this.searchOptions.push(caseTypeSearchOption);
      }
      if (this.tableFilters.CheckpointTypeCodes) {
        this.searchOptions.push(checkpointTypeSearchOption);
      }

      // now that we have the searchOptions, call parent's ngOnInit!
      super.ngOnInit();
    });
  }
}
