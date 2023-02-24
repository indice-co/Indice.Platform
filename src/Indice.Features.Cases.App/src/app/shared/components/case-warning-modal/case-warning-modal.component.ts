import { Component, OnInit } from '@angular/core';
import { Modal, ModalOptions } from '@indice/ng-components';
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
  public warningModalState: any;
  constructor(
    private modal: Modal,
    private options: ModalOptions) { }

  ngOnInit(): void {
    this.warningModalState = this.options?.initialState?.warningModalState;
  }

  public closeModal(accept: boolean = false) {
    this.modal.hide(accept);
  }
}
