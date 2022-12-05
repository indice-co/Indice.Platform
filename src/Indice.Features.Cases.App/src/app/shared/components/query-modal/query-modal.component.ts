import { Component, OnInit } from '@angular/core';
import { Modal } from '@indice/ng-components';
import { CasesApiService, SaveQueryRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-query-modal',
  templateUrl: './query-modal.component.html'
})
export class QueriesModalComponent implements OnInit {
  public friendlyName: string | undefined;

  constructor(
    private _api: CasesApiService,
    private modal: Modal
  ) { }

  ngOnInit(): void { }

  public closeModal() {
    this.modal.hide();
  }

  public saveQuery() {
    let href = window.location.href;
    let queryParameters = href.substring(href.indexOf('?'))
    this._api.saveQuery(undefined, new SaveQueryRequest({ friendlyName: this.friendlyName, parameters: queryParameters }))
      .subscribe(
        (_ => this.modal.hide())
      )
  }

}
