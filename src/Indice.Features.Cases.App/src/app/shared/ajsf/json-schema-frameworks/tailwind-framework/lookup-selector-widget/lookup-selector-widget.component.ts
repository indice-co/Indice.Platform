import { buildTitleMap, isArray, JsonSchemaFormService } from "@ajsf-extended/core";
import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { Subject } from "rxjs";
import { take, takeUntil } from "rxjs/operators";
import { CaseDetailsService } from "src/app/core/services/case-details.service";
import { CaseDetails, CasesApiService, LookupItemResultSet } from "src/app/core/services/cases-api.service";

@Component({
  selector: 'app-lookup-selector-widget',
  templateUrl: './lookup-selector-widget.component.html'
})
export class LookupSelectorWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  /**
  * Holds the name of the field that the widget is dependent on.
  */
  independentFieldName: string | undefined;
  /**
  * Holds the current value of the field that the widget is dependent on.
  */
  independentFieldValue: any;
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
    // Get lookup's Category
    let lookupCategory = this.options['lookup-category'];
    // Is the widget dependent on another form field?
    this.independentFieldName = this.options['independentField'];
    if (this.independentFieldName) {
      // Widget is dependent: Get the current value of independent Field
      this.independentFieldValue = this.jsf.data[this.independentFieldName];
    }
    // now, fetch case's Details to get customer Id
    this._caseDetailsService.caseDetails$.pipe(
      takeUntil(this.destroy$),
      take(1) // we really only need the first emitted value of the source Observable to get customerId
    )
      .subscribe((caseDetails: CaseDetails) => {
        this.customerId = caseDetails.customerId;
        if (!this.independentFieldName || (this.independentFieldName && this.independentFieldValue)) {
          // notice that, we always send the customerId.
          // Not really a problem: it can be ignored server-side if not needed.
          this._api.getLookup(lookupName, this.customerId, this.independentFieldValue, lookupCategory).pipe(
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
        if (this.independentFieldName) {
          this.jsf.dataChanges.pipe(
            takeUntil(this.destroy$)
          ).subscribe(
            // form data changed!
            (formData: any) => {
              // did independent Field data change?
              if (formData[this.independentFieldName!] != this.independentFieldValue) {
                // set formControl null - not crazy about that...
                this.formControl = null;
                this.jsf.formGroup.value[this.controlName!] = null;
                this.jsf.formGroup.controls[this.controlName!].value = null;
                // update current independent Field Value
                this.independentFieldValue = formData[this.independentFieldName!];
                // get the new lookups
                this._api.getLookup(lookupName, this.customerId, this.independentFieldValue, lookupCategory).pipe(
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

}
