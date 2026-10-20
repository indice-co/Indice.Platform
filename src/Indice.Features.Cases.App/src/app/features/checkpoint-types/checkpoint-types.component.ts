import { Component, OnInit, ChangeDetectionStrategy, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, ModalService, RouterViewAction, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { CheckpointTypeDeleteModalComponent } from './checkpoint-type-delete-modal/checkpoint-type-delete-modal.component';

@Component({
    selector: 'app-checkpoint-types',
    templateUrl: './checkpoint-types.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CheckpointTypesComponent extends BaseListComponent<CheckpointType> implements OnInit {
  /** When set, the list is scoped to the given case type (embedded mode inside the case type edit page). */
  @Input() public caseTypeId: string | undefined;
  /** Hides the page layout chrome when embedded. */
  @Input() public embedded = false;

  public newItemLink = 'checkpoint-types/create';
  public caseTypes: CaseTypePartial[] = [];
  public selectedCaseTypeId: string | undefined;
  public formActions: ViewAction[] = [];

  constructor(
    private _api: CasesApiService,
    private _route: ActivatedRoute,
    private _router: Router,
    private _modalService: ModalService,
    private _caseTypeService: CaseTypeService
  ) {
    super(_route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'caseTypeCode';
    this.sortdir = 'asc';
  }

  ngOnInit(): void {
    this.selectedCaseTypeId = this.caseTypeId ?? this._route.snapshot.queryParamMap.get('caseTypeId') ?? undefined;
    this.formActions = [
      new RouterViewAction(Icons.Add, '/checkpoint-types/create', null, null)
    ];
    if (!this.caseTypeId) {
      this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe(result => this.caseTypes = result.items ?? []);
    }
    super.ngOnInit();
  }

  loadItems(): Observable<IResultSet<CheckpointType> | null | undefined> {
    return this._api.getCheckpointTypesList(
      this.page,
      this.pageSize,
      this.sort ? (this.sortdir === 'desc' ? `${this.sort}-` : this.sort) : undefined,
      this.search || undefined,
      this.selectedCaseTypeId || undefined
    ).pipe(map(result => result as IResultSet<CheckpointType>));
  }

  public caseTypeChanged(caseTypeId: string | undefined): void {
    this.selectedCaseTypeId = caseTypeId || undefined;
    this.page = 1;
    this.refresh();
  }

  public createQueryParams(): { [key: string]: string } {
    return this.selectedCaseTypeId ? { caseTypeId: this.selectedCaseTypeId } : {};
  }

  public openDeleteModal(item: CheckpointType): void {
    const modal = this._modalService.show(CheckpointTypeDeleteModalComponent, {
      backdrop: 'static',
      keyboard: false,
      initialState: { id: item.id, code: item.code }
    });
    modal.onHidden?.subscribe(result => {
      if ((result as any)?.result) {
        this.refresh();
      }
    });
  }
}
