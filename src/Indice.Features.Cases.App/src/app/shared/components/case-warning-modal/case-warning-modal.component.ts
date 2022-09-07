import { Component, OnInit } from '@angular/core';
import { Modal } from '@indice/ng-components';

@Component({
  selector: 'app-case-warning-modal',
  templateUrl: './case-warning-modal.component.html'
})
/**
 * Displays a warning modal (with the red triangle icon)
 * Should be filled with title and description
 */
export class CaseWarningModalComponent implements OnInit {
  public title: string | undefined;
  public description: string | undefined;
  constructor(private modal: Modal) { }

  ngOnInit(): void {
  }

  public closeModal(accept: boolean = false) {
    this.modal.hide(accept);
  }
}
