import { buildTitleMap, isArray, JsonSchemaFormService } from "@ajsf-extended/core";
import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { CaseDetailsService } from "src/app/core/services/case-details.service";
import { CaseDetails, CasesApiService, LookupItemResultSet } from "src/app/core/services/cases-api.service";

@Component({
  selector: 'app-product-selector-widget',
  templateUrl: './product-selector-widget.component.html'
})
export class ProductSelectorWidgetComponent implements OnInit {
  formControl: any;
  controlName: string | undefined;
  controlValue: string | undefined;
  dependentField: string | undefined;
  dependentFieldCurrentValue: any;
  caseDetails: CaseDetails | undefined;
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
    this.dependentField = this.options['dependentField'];
    if (this.dependentField) {
      this.dependentFieldCurrentValue = this.jsf.data[this.dependentField];
    }
    let lookupName = this.options['lookup-name'] ?? this.controlName;
    let productFamily = this.options['productFamily'] ?? 'Account';
    this._caseDetailsService.caseDetails$.pipe(
      takeUntil(this.destroy$)
    )
      .subscribe((caseDetails: CaseDetails) => {
        this.caseDetails = caseDetails;
        if (!this.dependentField || (this.dependentField && this.dependentFieldCurrentValue)) {

          this._api.getLookup(lookupName, caseDetails.customerId!, this.dependentFieldCurrentValue).pipe(
            takeUntil(this.destroy$)
          ).subscribe(
            (lookUpItems: LookupItemResultSet) => {
              this.options.enumNames = lookUpItems.items!.map((i: any) => i.name);
              this.options.enum = lookUpItems.items!.map((i: any) => i.value);
              this.selectList = buildTitleMap(
                this.options.enumNames,
                this.options.enum,
                !!this.options.required,
                !!this.options.flatList
              );
              this.jsf.initializeControl(this);
              this.changeDetector.detectChanges();
            }
          );

        } else {
          this.jsf.initializeControl(this);
          this.changeDetector.detectChanges();
        }

      });
    if (this.dependentField) {
      this.jsf.dataChanges.pipe(
        takeUntil(this.destroy$)
      ).subscribe(
        (formData: any) => {
          if (formData[this.dependentField!] != this.dependentFieldCurrentValue) {
            this.dependentFieldCurrentValue = formData[this.dependentField!];
            this._api.getLookup(lookupName, this.caseDetails!.customerId!, this.dependentFieldCurrentValue).pipe(
              takeUntil(this.destroy$)
            ).subscribe(
              (lookUpItems: any) => {
                this.options.enumNames = lookUpItems.items.map((i: any) => i.name);
                this.options.enum = lookUpItems.items.map((i: any) => i.value);
                this.selectList = buildTitleMap(
                  this.options.enumNames,
                  this.options.enum,
                  !!this.options.required,
                  !!this.options.flatList
                );
                this.changeDetector.detectChanges();
                this.jsf.validateData(formData);
              }
            );
          }
        }
      );
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

}
