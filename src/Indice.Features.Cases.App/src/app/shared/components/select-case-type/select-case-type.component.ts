import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { CasesApiService, CaseType } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-select-case-type',
  templateUrl: './select-case-type.component.html',
  styleUrls: ['./select-case-type.component.scss']
})
export class SelectCaseTypeComponent implements OnInit {
  public caseTypes$: Observable<CaseType[]>;
  public caseType = '';
  @Output() selectedCaseTypeEvent = new EventEmitter<string>();

  constructor(private api: CasesApiService) {
    this.caseTypes$ = this.api.getCaseTypes()
  }

  ngOnInit(): void { }

  onSelect(value: any) {
    this.selectedCaseTypeEvent.emit(value);
  }
}
