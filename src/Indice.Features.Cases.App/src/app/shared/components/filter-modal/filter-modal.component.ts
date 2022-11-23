import { Component, OnInit } from '@angular/core';
import { Modal } from '@indice/ng-components';
import { CasesApiService, SaveFilterRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-filter-modal',
  templateUrl: './filter-modal.component.html'
})
export class FiltersModalComponent implements OnInit {
  public filterName: string | undefined;

  constructor(
    private _api: CasesApiService,
    private modal: Modal) { }

  ngOnInit(): void { }

  public closeModal() {
    let href = window.location.href;
    let queryParameters = href.substring(href.indexOf('?'))
    let saveFilterRequest = new SaveFilterRequest({ name: this.filterName, queryParameters: queryParameters });
    this._api.saveFilter(undefined, saveFilterRequest)
      .subscribe(
        (_ => this.modal.hide())
      )
  }
}
