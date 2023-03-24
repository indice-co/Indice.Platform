import { buildTitleMap, isArray, JsonSchemaFormService } from "@ajsf-extended/core";
import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import * as _ from "lodash";
import { Subject } from "rxjs";
import { take, takeUntil } from "rxjs/operators";
import { CaseDetailsService } from "src/app/core/services/case-details.service";
import { Case, CasesApiService, FilterTerm, LookupItemResultSet } from "src/app/core/services/cases-api.service";

@Component({
  selector: 'app-lookup-selector-widget',
  templateUrl: './lookup-selector-widget.component.html'
})
export class LookupSelectorWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  /**
  * Holds the name(s) of the field(s) that the widget/field is dependent on.
  */
  private lookupFilterFields: string[] | undefined;
  /**
  * Holds the current value(s) of the field(s) that the widget/field is dependent on.
  */
  private lookupFilterFieldValues: any = {};
  /**
  * Holds the "constant" part of Filter Terms.
  */
  private lookupFilterTerms: FilterTerm[] = [];
  /**
  * This widget is part of a form.
  * The form is part of a specific case.
  * The case has details.
  * One of those details is customerId.
  */
  private customerId: string | undefined;
  controlDisabled: boolean = false;
  options: any;
  autoCompleteList: string[] = [];
  selectList: any[] = [];
  isArray = isArray;
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] | undefined;
  @Input() dataIndex: number[] | undefined;

  private destroy$ = new Subject();

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _caseDetailsService: CaseDetailsService,
    private _api: CasesApiService,
    private _jsf: JsonSchemaFormService
  ) { }

  ngOnInit(): void {
    this.options = this.layoutNode.options || {};
    // early exit to avoid lookup call
    if (this.options.condition === 'false') {
      return
    }
    // Get lookup's Name
    let lookupName = this.options['lookup-name'];
    // Is the widget dependent on another form field(s)?
    this.lookupFilterFields = this.options['lookup-filter-fields'];
    // is Widget dependent? Get the current value of independent Fields
    this.lookupFilterFields?.forEach((x) => this.lookupFilterFieldValues[x] = this._jsf.data[x]);
    // now, fetch case's Details to get customer Id
    this._caseDetailsService.caseDetails$.pipe(
      takeUntil(this.destroy$),
      take(1) // we really only need the first emitted value of the source Observable to get customerId
    ).subscribe((caseDetails: Case) => {
      this.customerId = caseDetails.customerId;
      // create the "constant" part of Filter Terms.
      this.lookupFilterTerms = this.createFilterTerms();
      if (!this.lookupFilterFields || (this.lookupFilterFields && !_.isEmpty(this.lookupFilterFieldValues) && this.allPropertiesHaveValues(this.lookupFilterFieldValues))) {
        let lookupFilterTerms: FilterTerm[] = this.addFilterFieldValuesToFilterTerms();
        // get lookUp Items - notice we don't use the caching service!
        this._api.getLookup(lookupName, lookupFilterTerms).pipe(
          takeUntil(this.destroy$),
          take(1)
        ).subscribe(
          (lookUpItems: LookupItemResultSet) => {
            this.createSelectList(lookUpItems);
            this._jsf.initializeControl(this);
            // enforce change Detection
            this._changeDetector.detectChanges();
          }
        );
      } else {
        this._jsf.initializeControl(this);
      }
      // Additionally: If the widget dependent on another form field, subscribe to form data Changes!
      if (this.lookupFilterFields) {
        this._jsf.dataChanges.pipe(
          takeUntil(this.destroy$)
        ).subscribe(
          // form data changed!
          (formData: any) => {
            // did independent Field data change?
            let filterFieldValueChanged: boolean = false;
            for (const key in this.lookupFilterFieldValues) {
              if (formData[key] && formData[key] != this.lookupFilterFieldValues[key]) {
                filterFieldValueChanged = true;
                // update current independent Field Value
                this.lookupFilterFieldValues[key] = formData[key];
              }
            }
            if (filterFieldValueChanged) {
              // set formControl null - not crazy about that...
              this.formControl = null;
              this._jsf.formGroup.value[this.controlName!] = null;
              this._jsf.formGroup.controls[this.controlName!].value = null;
              // "re-init" lookupFilterTerms
              let lookupFilterTerms: FilterTerm[] = this.addFilterFieldValuesToFilterTerms();
              // get new lookUp Items
              this._api.getLookup(lookupName, lookupFilterTerms).pipe(
                takeUntil(this.destroy$)
              ).subscribe(
                (lookUpItems: LookupItemResultSet) => {
                  this.createSelectList(lookUpItems);
                  // re-initialize Control
                  this._jsf.initializeControl(this);
                  // enforce change Detection
                  this._changeDetector.detectChanges();
                  // enforce form data Validation
                  this._jsf.validateData(formData);
                }
              );
            }
          }
        );
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private addFilterFieldValuesToFilterTerms(): FilterTerm[] {
    let lookupFilterTerms: FilterTerm[] = [];
    lookupFilterTerms = lookupFilterTerms.concat(this.lookupFilterTerms);
    if (this.lookupFilterFields) {
      for (const key in this.lookupFilterFieldValues) {
        lookupFilterTerms.push(new FilterTerm({ key: key, value: this.lookupFilterFieldValues[key] }));
      }
    }
    return lookupFilterTerms;
  }

  private createFilterTerms(): FilterTerm[] {
    let lookupFilterTerms: FilterTerm[] = [];
    // do we have parameters passed from layout configuration?
    if (this.options['lookup-filter-terms']) {
      // deep clone the array (we don't want the options to be changed)
      lookupFilterTerms = _.cloneDeep(this.options['lookup-filter-terms']);
    }
    // notice that, we always send a customerId.
    // Not really a problem: it can be ignored server-side if not needed.
    lookupFilterTerms.push(new FilterTerm({ key: 'customerId', value: this.customerId }));
    return lookupFilterTerms;
  }

  private createSelectList(lookUpItems: LookupItemResultSet): void {
    this.options.enumNames = lookUpItems?.items?.map((i: any) => i.name);
    this.options.enum = lookUpItems?.items?.map((i: any) => i.value);
    this.selectList = buildTitleMap(
      this.options.enumNames,
      this.options.enum,
      !!this.options.required,
      !!this.options.flatList
    );
  }

  private allPropertiesHaveValues(obj: any): boolean {
    for (const key in obj) {
      if (obj[key] == null || obj[key] == undefined) {
        return false;
      }
    }
    return true;
  }

}
