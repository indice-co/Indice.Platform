import { map, take } from 'rxjs/operators';
import { CasesApiService, CaseTypePartial, CaseTypePartialResultSet } from './../../core/services/cases-api.service';
import { Component, OnInit } from '@angular/core';
import { BaseListComponent, Icons, IResultSet, RouterViewAction, ViewAction, ListViewType } from '@indice/ng-components';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, of } from 'rxjs';

@Component({
  selector: 'app-case-types',
  templateUrl: './case-types.component.html',
  styleUrls: ['./case-types.component.scss']
})
export class CaseTypesComponent extends BaseListComponent<CaseTypePartial> implements OnInit {

  public newItemLink = 'case-types/create';
  public formActions: ViewAction[] = [
    new RouterViewAction(Icons.Add, this.newItemLink, null, null)
  ];

  constructor(
    private _api: CasesApiService,
    private _route: ActivatedRoute,
    private _router: Router
  ) {
    super(_route, _router)
    this.view = ListViewType.Table
  }

  ngOnInit(): void {
    super.ngOnInit();
  }

  loadItems(): Observable<IResultSet<CaseTypePartial> | null | undefined> {
    return this._api.getCaseTypes()
      .pipe(
        take(1),
        map((result: CaseTypePartialResultSet) => (result as IResultSet<CaseTypePartial>))
      )
  }
}
