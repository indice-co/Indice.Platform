import { JsonSchemaFormService } from "@ajsf-extended/core";
import { Component, Input, OnInit } from '@angular/core';
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

  constructor(
    private _lookupsService: LookupsService,
    private jsf: JsonSchemaFormService
  ) { }

  ngOnInit(): void {
    this.options = this.layoutNode.options || {};
    this.jsf.initializeControl(this);
    this.searchTerm = this.formControl.value;
    this.options.typeahead = {};
    this.options.typeahead.source = [];
    let lookupName = this.options['lookup-name'] ?? this.controlName;
    this._lookupsService.getLookup(lookupName)
      .subscribe(
        (lookUpItems: LookupItemResultSet) => {
          for (let i = 0; i < lookUpItems?.count! - 1; i++) {
            this.options.typeahead.source.push(lookUpItems?.items![i].value + ' - ' + lookUpItems?.items![i].name);
          }
        }
      );
    this.formControl.valueChanges.subscribe(
      (value: any) => {
        this.searchTerm = value;
      }
    );
  }

  updateValue(event: any) {
    this.jsf.updateValue(this, event.target.value);
  }

  updateCode() {
    if (!this.options.typeahead.source.includes(this.searchTerm)) {
      this.searchTerm = '';
    }
    this.jsf.updateValue(this, this.searchTerm.substring(0, this.searchTerm.indexOf(' ')));
  }

}
