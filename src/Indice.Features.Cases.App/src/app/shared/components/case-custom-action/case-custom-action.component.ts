import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CustomCaseAction, SuccessMessage } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-custom-action',
  templateUrl: './case-custom-action.component.html',
  styleUrls: ['./case-custom-action.component.scss']
})
export class CaseCustomActionComponent implements OnInit {

  @Input() action: CustomCaseAction | undefined;
  @Output() actionTriggered = new EventEmitter<{ redirectToList: boolean | undefined, successMessage: SuccessMessage | undefined, id: string | undefined, value: string | undefined }>();
  buttonDisabled = false;
  value: string | undefined;
  class: string | undefined;

  constructor() { }

  ngOnInit(): void {
    this.value = this.action?.defaultValue;
    this.class = this.action?.class ?? 'btn-info';
  }

  triggerAction() {
    this.buttonDisabled = true;
    this.actionTriggered.emit({
      redirectToList: this.action?.redirectToList,
      successMessage: this.action?.successMessage,
      id: this.action?.id!,
      value: this.value!
    });
  }
}
