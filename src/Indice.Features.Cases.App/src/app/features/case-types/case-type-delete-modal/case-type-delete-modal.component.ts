import { catchError, tap } from 'rxjs/operators';
import { Component, OnInit } from '@angular/core';
import { Modal, ModalOptions, ToastType } from '@indice/ng-components';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { EMPTY } from 'rxjs';

@Component({
    selector: 'app-case-type-delete-modal',
    templateUrl: './case-type-delete-modal.component.html',
    styleUrls: ['./case-type-delete-modal.component.css'],
    standalone: false
})
export class CaseTypeDeleteModalComponent implements OnInit {
  public id: any = '';
  public name?: string;
  constructor(
    private modal: Modal,
    private _api: CasesApiService,
    private toaster: TranslatedToasterService,
    private options: ModalOptions) { }

  ngOnInit(): void {
    this.id = this.options?.initialState?.id;
  }

  deleteCaseType() {
    this._api.deleteCaseType(this.id).pipe(
      tap(_ => {
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeDeleted.body');
        this.closeModal(true);
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, 'toasts.error.title', err.detail);
        this.closeModal(false);
        return EMPTY
      })
    ).subscribe();
  }

  public closeModal(result: any) {
    this.modal.hide(result);
  }
}
