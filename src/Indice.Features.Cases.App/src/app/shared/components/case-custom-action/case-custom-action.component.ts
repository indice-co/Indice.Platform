import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CustomCaseAction, IActionRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-custom-action',
  templateUrl: './case-custom-action.component.html',
  styleUrls: ['./case-custom-action.component.scss']
})
export class CaseCustomActionComponent implements OnInit {

  @Input() action: CustomCaseAction | undefined;
  @Output() actionTriggered = new EventEmitter<IActionRequest>();
  buttonDisabled = false;
  value: string | undefined;

  constructor() { }

  ngOnInit(): void {
    this.value = this.action?.defaultValue;
   }

  triggerAction() {
    this.buttonDisabled = true;
    this.actionTriggered.emit({
      id: this.action?.id!,
      value: this.value!
    });
  }
}
