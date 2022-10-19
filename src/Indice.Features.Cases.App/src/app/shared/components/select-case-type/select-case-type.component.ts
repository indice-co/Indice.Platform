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
  public caseType = new CaseTypePartial;
  @Output() selectedCaseTypeEvent = new EventEmitter<CaseTypePartial>();

  constructor(private api: CasesApiService) {
    this.caseTypes$ = this.api.getCaseTypes(true).pipe(
      map((result: CaseTypePartialResultSet) => result.items as CaseTypePartial[])
    )
  }

  ngOnInit(): void { }

  onSelect(value: CaseTypePartial) {
    this.selectedCaseTypeEvent.emit(value);
  }
}
