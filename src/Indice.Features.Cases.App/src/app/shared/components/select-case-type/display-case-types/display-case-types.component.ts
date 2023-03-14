import { Component, Input, OnInit, EventEmitter, Output } from '@angular/core';
import { CaseTypePartial } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-display-case-types',
  templateUrl: './display-case-types.component.html',
  styleUrls: ['./display-case-types.component.scss']
})
export class DisplayCaseTypesComponent implements OnInit {
  @Input() public caseTypes: CaseTypePartial[]  = [];
  @Output() selectedCaseTypeEvent = new EventEmitter<CaseTypePartial>();

  constructor() { }

  ngOnInit(): void { }

  public onSelect(caseType:any) {
    this.selectedCaseTypeEvent.emit(caseType)
  }
}
