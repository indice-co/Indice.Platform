import { JsonSchemaFormService } from "@ajsf-extended/core";
import { Component, Input, OnInit } from '@angular/core';
import { Subject } from "rxjs";
import { takeUntil, tap } from "rxjs/operators";
import { LookupItemResultSet } from "src/app/core/services/cases-api.service";
import { LookupsService } from "src/app/core/services/lookups.service";

@Component({
  selector: 'app-lookup-widget',
  templateUrl: './lookup-widget.component.html',
  styleUrls: ['./lookup-widget.component.scss']
})
export class LookupWidgetComponent implements OnInit {
  formControl: any;
  controlName: string = '';
  controlValue: string | undefined;
  controlDisabled: boolean = false;
  boundControl: boolean = false;
  options: any;
  autoCompleteList: string[] = [];
  @Input() layoutNode: any;
  @Input() layoutIndex: number[] | undefined;
  @Input() dataIndex: number[] | undefined;

  occupations: LookupItemResultSet | undefined;
  searchTerm: any;
  separator = '-';
  private destroy$ = new Subject();

  constructor(
    private _lookupsService: LookupsService,
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit(): void {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);
    this.options.typeahead = {};
    this.options.typeahead.source = [];
    let lookupName = this.options['lookup-name'] ?? this.controlName;
    this._lookupsService.getLookup(lookupName).pipe(
      takeUntil(this.destroy$)
    ).subscribe(
      (lookUpItems: LookupItemResultSet) => {
        for (let i = 0; i < lookUpItems?.count! - 1; i++) {
          this.options.typeahead.source.push(`${lookUpItems?.items![i].value} ${this.separator} ${lookUpItems?.items![i].name}`);
        }
        // initialize searchTerm
        if (this.formControl.value) {
          this.searchTerm = this.options.typeahead.source.find((s: string) => s.startsWith(`${this.formControl.value} ${this.separator}`));
        }
      }
    );
    // subscribe to formControl value Changes in order to inform UI
    this.formControl.valueChanges.pipe(
      takeUntil(this.destroy$),
      tap((value: any) => {
        this.searchTerm = (value !== undefined && value !== null) ?
          this.options.typeahead.source.find((s: string) => s.startsWith(`${value} ${this.separator}`)) :
          value;
      })
    ).subscribe();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // updateValue(event: any) {
  //   this.jsf.updateValue(this, event.target.value);
  // }

  updateValue() {
    if (!this.options.typeahead.source.includes(this.searchTerm)) {
      this.searchTerm = '';
    }
    this.jsf.updateValue(this, this.searchTerm.substring(0, this.searchTerm.indexOf(' ')));
  }

}
