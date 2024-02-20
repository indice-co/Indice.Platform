import { PageIllustrationComponent } from './../page-illustration/page-illustration.component';
import { CaseTypePartialResultSet, CustomerDetails } from './../../../core/services/cases-api.service';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { CasesApiService, CaseTypePartial } from 'src/app/core/services/cases-api.service';
import { map } from 'rxjs/internal/operators';

@Component({
  selector: 'app-select-case-type',
  templateUrl: './select-case-type.component.html',
  styleUrls: ['./select-case-type.component.scss']
})
export class SelectCaseTypeComponent implements OnInit {
  public caseTypes$: Observable<CaseTypePartial[]>;
  public selectedCaseTypeCode = '';
  public selectedCaseType: CaseTypePartial = new CaseTypePartial();
  @Output() selectedCaseTypeEvent = new EventEmitter<string>();
  @Output() sidePanelTitleEvent = new EventEmitter<string>();
  @Output() selectedCustomerEvent = new EventEmitter<CustomerDetails>();


  constructor(private api: CasesApiService) {
    this.caseTypes$ = this.api.getCaseTypes(true).pipe(
      map((result: CaseTypePartialResultSet) => result.items as CaseTypePartial[])
    )
  }

  ngOnInit(): void { }

  onSelect(value:any) {
    this.selectedCaseType = value;
    this.selectedCaseTypeCode = this.selectedCaseTypeCode === value.code ? '' : value.code
    this.selectedCaseTypeEvent.emit(this.selectedCaseTypeCode);
    if (this.selectedCaseTypeCode) {
      this.sidePanelTitleEvent.emit('Υποβολή Υπόθεσης - Επιλογή πελάτη');
    } else {
      this.sidePanelTitleEvent.emit('');
      this.selectedCustomerEvent.emit();
    }
  }
}
