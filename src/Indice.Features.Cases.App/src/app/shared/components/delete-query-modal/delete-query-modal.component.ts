import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Modal, ModalOptions } from '@indice/ng-components';
import { CasesApiService } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-delete-query-modal',
  templateUrl: './delete-query-modal.component.html'
})
export class DeleteQueryModalComponent implements OnInit {
  public query: any;

  constructor(
    private _api: CasesApiService,
    private _router: Router,
    private _modal: Modal,
    private _options: ModalOptions
  ) { }

  ngOnInit(): void {
    this.query = this._options?.initialState?.query;
  }

  deleteQuery(): void {
    this._api.deleteQuery(this.query?.id!).subscribe(
      (_) => {
        this.closeModal()
        this.redirectTo('/cases');
      }
    )
  }

  public closeModal() {
    this._modal.hide();
  }

  private redirectTo(url: string) {
    // https://stackoverflow.com/a/49509706/19162333
    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() =>
      this._router.navigateByUrl(url));
  }

}
