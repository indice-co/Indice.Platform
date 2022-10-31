import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CustomCaseAction, ICustomActionTrigger } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-custom-action',
  templateUrl: './case-custom-action.component.html',
  styleUrls: ['./case-custom-action.component.scss']
})
export class CaseCustomActionComponent implements OnInit {

  @Input() action: CustomCaseAction | undefined;
  @Output() actionTriggered = new EventEmitter<ICustomActionTrigger>();

  value: string | undefined;

  constructor() { }

  ngOnInit(): void { }

  triggerAction() {
    this.actionTriggered.emit({
      id: this.action?.id!,
      value: this.value!
    });
  }
}
