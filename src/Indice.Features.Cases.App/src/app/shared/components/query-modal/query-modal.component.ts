import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Modal } from '@indice/ng-components';
import { CasesApiService, SaveQueryRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-query-modal',
  templateUrl: './query-modal.component.html'
})
export class QueriesModalComponent implements OnInit {
  public friendlyName: string | undefined;
  public queryParameters: string | undefined;

  constructor(
    private _route: ActivatedRoute,
    private _api: CasesApiService,
    private modal: Modal
  ) { }

  ngOnInit(): void {
    // get a snapshot of queryParam
    let paramMap = this._route.snapshot.queryParamMap;
    // re-construct queryParameters to ensure that no user-added params are send to server & page will always be 1!
    this.queryParameters = `?view=${paramMap.get('view')}&page=1&pagesize=${paramMap.get('pagesize')}&search=${paramMap.get('search')}&sort=${paramMap.get('sort')}&dir=${paramMap.get('dir')}&filter=${paramMap.get('filter')}`
  }

  public closeModal() {
    this.modal.hide();
  }

  public saveQuery() {
    this._api.saveQuery(new SaveQueryRequest({ friendlyName: this.friendlyName, parameters: this.queryParameters }))
      .subscribe(
        (_ => this.modal.hide())
      )
  }

}
