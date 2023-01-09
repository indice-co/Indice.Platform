import { buildTitleMap, isArray, JsonSchemaFormService } from "@ajsf-extended/core";
import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import * as _ from "lodash";
import { Subject } from "rxjs";
import { take, takeUntil } from "rxjs/operators";
import { CaseDetailsService } from "src/app/core/services/case-details.service";
import { CaseDetails, CasesApiService, FilterTerm, LookupItemResultSet } from "src/app/core/services/cases-api.service";

@Component({
  selector: 'app-lookup-selector-widget',
  templateUrl: './lookup-selector-widget.component.html'
})
export class LookupSelectorWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  /**
  * Holds the name(s) of the field(s) that the widget is dependent on.
  */
  lookupFilterFields: string[] | undefined;
  /**
  * Holds the current value(s) of the field(s) that the widget is dependent on.
  */
  lookupFilterFieldValues: any = {};
  /**
  * This widget is part of a form.
  * The form is part of a specific case.
  * The case has details.
  * One of those details is customerId.
  */
  customerId: string | undefined;
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
    public changeDetector: ChangeDetectorRef,
    private _caseDetailsService: CaseDetailsService,
    private _api: CasesApiService,
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit(): void {
    this.options = this.layoutNode.options || {};
    // Get lookup's Name
    let lookupName = this.options['lookup-name'] ?? this.controlName;
    // Get lookup's Filter Terms
    let lookupFilterTerms: FilterTerm[] = [];
    if (this.options['lookup-filter-terms']) {
      // clone the array
      lookupFilterTerms = JSON.parse(JSON.stringify(this.options['lookup-filter-terms']))
    }
    // Is the widget dependent on another form field(s)?
    this.lookupFilterFields = this.options['lookup-filter-fields'];
    // is Widget dependent? Get the current value of independent Fields
    this.lookupFilterFields?.forEach((x) => this.lookupFilterFieldValues[x] = this.jsf.data[x]);
    // now, fetch case's Details to get customer Id
    this._caseDetailsService.caseDetails$.pipe(
      takeUntil(this.destroy$),
      take(1) // we really only need the first emitted value of the source Observable to get customerId
    )
      .subscribe((caseDetails: CaseDetails) => {
        this.customerId = caseDetails.customerId;
        if (!this.lookupFilterFields || (this.lookupFilterFields && !_.isEmpty(this.lookupFilterFieldValues) && this.propertiesHaveValues(this.lookupFilterFieldValues))) {
          // notice that, we always send a customerId.
          // Not really a problem: it can be ignored server-side if not needed.
          lookupFilterTerms.push(new FilterTerm({ key: 'customerId', value: this.customerId }));
          if (this.lookupFilterFields) {
            for (const key in this.lookupFilterFieldValues) {
              lookupFilterTerms.push(new FilterTerm({ key: key, value: this.lookupFilterFieldValues[key] }));
            }
          }
          this._api.getLookup(lookupName, lookupFilterTerms).pipe(
            takeUntil(this.destroy$)
          ).subscribe(
            // get lookUp Items
            (lookUpItems: LookupItemResultSet) => {
              // create the selectList
              this.options.enumNames = lookUpItems?.items?.map((i: any) => i.name);
              this.options.enum = lookUpItems?.items?.map((i: any) => i.value);
              this.selectList = buildTitleMap(
                this.options.enumNames,
                this.options.enum,
                !!this.options.required,
                !!this.options.flatList
              );
              this.jsf.initializeControl(this);
              // enforce change Detection
              this.changeDetector.detectChanges();
            }
          );
        } else {
          this.jsf.initializeControl(this);
        }
        // Additionally: If the widget dependent on another form field, subscribe to form data Changes!
        if (this.lookupFilterFields) {
          this.jsf.dataChanges.pipe(
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
                this.jsf.formGroup.value[this.controlName!] = null;
                this.jsf.formGroup.controls[this.controlName!].value = null;
                // "re-init" lookupFilterTerms
                let lookupFilterTerms: FilterTerm[] = [];
                if (this.options['lookup-filter-terms']) {
                  lookupFilterTerms = JSON.parse(JSON.stringify(this.options['lookup-filter-terms']))
                }
                // get the new lookups
                lookupFilterTerms.push(new FilterTerm({ key: 'customerId', value: this.customerId }));
                if (this.lookupFilterFields) {
                  for (const key in this.lookupFilterFieldValues) {
                    lookupFilterTerms.push(new FilterTerm({ key: key, value: this.lookupFilterFieldValues[key] }));
                  }
                }
                this._api.getLookup(lookupName, lookupFilterTerms).pipe(
                  takeUntil(this.destroy$)
                ).subscribe(
                  (lookUpItems: LookupItemResultSet) => {
                    // create the new selectList
                    this.options.enumNames = lookUpItems?.items?.map((i: any) => i.name);
                    this.options.enum = lookUpItems?.items?.map((i: any) => i.value);
                    this.selectList = buildTitleMap(
                      this.options.enumNames,
                      this.options.enum,
                      !!this.options.required,
                      !!this.options.flatList
                    );
                    // re-initialize Control
                    this.jsf.initializeControl(this);
                    // enforce change Detection
                    this.changeDetector.detectChanges();
                    // enforce form data Validation
                    this.jsf.validateData(formData);
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

  private propertiesHaveValues(obj: any): boolean {
    for (const key in obj) {
      if (obj[key] == null || obj[key] == undefined) {
        return false;
      }
    }
    return true;
  }

}
