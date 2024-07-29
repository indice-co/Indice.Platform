import { map, take } from 'rxjs/operators';
import { Component, OnInit } from '@angular/core';
import { BaseListComponent, Icons, IResultSet, RouterViewAction, ViewAction, ListViewType, ModalService } from '@indice/ng-components';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CaseTypePartial, CasesApiService, CaseTypePartialResultSet } from 'src/app/core/services/cases-api.service';
import { CaseTypeDeleteModalComponent } from './case-type-delete-modal/case-type-delete-modal.component';

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
    private _router: Router,
    private ModalService: ModalService,
    private caseTypeService: CaseTypeService
  ) {
    super(_route, _router)
    this.view = ListViewType.Table
  }

  ngOnInit(): void {
    super.ngOnInit();
  }

  loadItems(): Observable<IResultSet<CaseTypePartial> | null | undefined> {
    return this.caseTypeService.getCaseTypes()
      .pipe(
        take(1),
        map((result: CaseTypePartialResultSet) => (result as IResultSet<CaseTypePartial>))
      )
  }

  confirmDeleteModal(caseTypeId:string): void {
    const modal = this.ModalService.show(CaseTypeDeleteModalComponent, {
      backdrop: 'static',
      keyboard: false,
      initialState: {id: caseTypeId}
    });

    modal.onHidden?.subscribe(_ => {
      if ((_ as any).result) {
        this.refresh();
      }
    })
  }
}
