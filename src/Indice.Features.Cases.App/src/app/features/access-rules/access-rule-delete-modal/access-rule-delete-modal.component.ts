import { catchError, tap } from 'rxjs/operators';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Modal, ModalOptions, ToastType } from '@indice/ng-components';
import { EMPTY } from 'rxjs';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

@Component({
    selector: 'app-access-rule-delete-modal',
    templateUrl: './access-rule-delete-modal.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AccessRuleDeleteModalComponent implements OnInit {
  public id: string = '';

  constructor(
    private modal: Modal,
    private _api: CasesApiService,
    private toaster: TranslatedToasterService,
    private options: ModalOptions) { }

  ngOnInit(): void {
    this.id = this.options?.initialState?.id as string;
  }

  delete(): void {
    this._api.deleteAccessRule(this.id).pipe(
      tap(_ => {
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.accessRuleDeleted.body');
        this.closeModal(true);
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        this.closeModal(false);
        return EMPTY;
      })
    ).subscribe();
  }

  public closeModal(result: boolean): void {
    this.modal.hide(result);
  }
}
