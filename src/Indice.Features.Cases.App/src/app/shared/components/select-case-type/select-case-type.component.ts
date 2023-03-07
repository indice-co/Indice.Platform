import { PageIllustrationComponent } from './../page-illustration/page-illustration.component';
import { CaseTypePartialResultSet } from './../../../core/services/cases-api.service';
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
  @Output() selectedCaseTypeEvent = new EventEmitter<string>();
  @Output() sidePanelTitleEvent = new EventEmitter<string>();


  constructor(private api: CasesApiService) {
    this.caseTypes$ = this.api.getCaseTypes(true).pipe(
      map((result: CaseTypePartialResultSet) => result.items as CaseTypePartial[])
    )
  }

  ngOnInit(): void { }

  onSelect(value:any) {
    // this.selectedCaseTypeCode !== value ? value : '';
    if (this.selectedCaseTypeCode == value) {
      this.selectedCaseTypeCode = '';
    } else {
      this.selectedCaseTypeCode = value
    }
    this.selectedCaseTypeEvent.emit(this.selectedCaseTypeCode);
    if (this.selectedCaseTypeCode) {
      this.sidePanelTitleEvent.emit('Υποβολή Αίτησης - Επιλογή πελάτη')
    } else {
      this.sidePanelTitleEvent.emit('')
    }
  }
}
