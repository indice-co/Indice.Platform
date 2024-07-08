import { catchError, finalize, take, tap } from 'rxjs/operators';
import { Component, OnInit } from '@angular/core';
import { Modal, ModalOptions, ToasterService, ToastType } from '@indice/ng-components';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { EMPTY } from 'rxjs';

@Component({
  selector: 'app-case-type-delete-modal',
  templateUrl: './case-type-delete-modal.component.html',
  styleUrls: ['./case-type-delete-modal.component.scss']
})
export class CaseTypeDeleteModalComponent implements OnInit {
  public id: any = '';
  public name?: string;
  constructor(
    private modal: Modal,
    private _api: CasesApiService,
    private toaster: ToasterService,
    private options: ModalOptions) { }

  ngOnInit(): void {
    this.id = this.options?.initialState?.id;
  }

  deleteCaseType() {
    this._api.deleteCaseType(this.id).pipe(
      tap(_ => {
        this.toaster.show(ToastType.Success, "Επιτυχία!", "Η διαγραφή του τύπου υπόθεσης ολοκληρώθηκε");
        this.closeModal(true);
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, "Whoops!", err.detail);
        this.closeModal(false);
        return EMPTY
      }),
      take(1)
    ).subscribe();
  }

  public closeModal(result: any) {
    this.modal.hide(result);
  }
}
