import { Component, OnInit } from '@angular/core';
import { Modal } from '@indice/ng-components';
export class WarningViewModel {
  public title: string | undefined;
  public description: string | undefined;
}
@Component({
  selector: 'app-case-warning-modal',
  templateUrl: './case-warning-modal.component.html'
})
/**
 * Displays a warning modal (with the red triangle icon)
 * Should be filled with title and description
 */
export class CaseWarningModalComponent implements OnInit {
  public warningModalState: WarningViewModel | undefined;
  constructor(private modal: Modal) { }

  ngOnInit(): void {
  }

  public closeModal(accept: boolean = false) {
    this.modal.hide(accept);
  }
}
