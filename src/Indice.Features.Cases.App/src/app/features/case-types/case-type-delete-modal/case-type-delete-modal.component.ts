import { Component, Input, OnInit } from '@angular/core';
import { Modal, ToasterService, ToastType } from '@indice/ng-components';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-type-delete-modal',
  templateUrl: './case-type-delete-modal.component.html',
  styleUrls: ['./case-type-delete-modal.component.scss']
})
export class CaseTypeDeleteModalComponent implements OnInit {

  public id: string = '';
  public name?: string;
  constructor(private modal: Modal, private _api: CasesApiService, private toaster: ToasterService) { }

  ngOnInit(): void {
  }

  deleteCaseType() {
    this._api.deleteCaseType(this.id).subscribe(_ => {
      this.toaster.show(ToastType.Success, "Επιτυχία!", "Η διαγραφή του τύπου αίτησης ολοκληρώθηκε");
      window.location.reload();
    }, 
    (err) => {
      this.toaster.show(ToastType.Error, "Ουπς!", "Δεν μπορεί να διαγραφεί ο τύπος αίτησης καθώς υπάρχουν αιτήσεις με αυτόν");
      this.closeModal();
    })
  }

  public closeModal() {
    this.modal.hide();
  }
}
