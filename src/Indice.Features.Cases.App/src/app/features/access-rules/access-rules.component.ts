import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, ModalService, RouterViewAction, ViewAction } from '@indice/ng-components';
import { forkJoin, Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AccessRule, CasesApiService } from 'src/app/core/services/cases-api.service';
import { AccessRuleDeleteModalComponent } from './access-rule-delete-modal/access-rule-delete-modal.component';

export const ACCESS_LEVELS: { value: number; label: string }[] = [
  { value: 0, label: 'accessRules.level.read' },
  { value: 1, label: 'accessRules.level.manage' },
  { value: 2, label: 'accessRules.level.administer' }
];

@Component({
    selector: 'app-access-rules',
    templateUrl: './access-rules.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AccessRulesComponent extends BaseListComponent<AccessRule> implements OnInit {
  public newItemLink = 'access-rules/create';
  public formActions: ViewAction[] = [
    new RouterViewAction(Icons.Add, '/access-rules/create', null, null)
  ];
  public caseTypeCodes: { [id: string]: string } = {};
  public checkpointTypeCodes: { [id: string]: string } = {};
  public checkpointTypeOptions: { id: string; label: string }[] = [];
  public roleFilter: string | undefined;
  public groupFilter: string | undefined;
  public checkpointTypeFilter: string | undefined;

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
  }

  ngOnInit(): void {
    forkJoin({
      caseTypes: this._caseTypeService.getCaseTypes().pipe(take(1)),
      checkpointTypes: this._api.getCheckpointTypesList(1, 1000, 'caseTypeCode')
    }).subscribe(({ caseTypes, checkpointTypes }) => {
      (caseTypes.items ?? []).forEach(x => this.caseTypeCodes[x.id!] = x.code!);
      (checkpointTypes.items ?? []).forEach(x => this.checkpointTypeCodes[x.id!] = `${x.caseTypeCode} / ${x.code}`);
      this.checkpointTypeOptions = (checkpointTypes.items ?? []).map(x => ({ id: x.id!, label: `${x.caseTypeCode} / ${x.code}` }));
      super.ngOnInit();
    });
  }

  loadItems(): Observable<IResultSet<AccessRule> | null | undefined> {
    return this._api.getAccessRules(
      this.page,
      this.pageSize,
      undefined,
      undefined,
      this.roleFilter ? `role::eq::${this.roleFilter}` : undefined,
      this.groupFilter ? `groupId::eq::${this.groupFilter}` : undefined,
      this.checkpointTypeFilter ? `checkpoint::eq::${this.checkpointTypeFilter}` : undefined,
      undefined
    ).pipe(map(result => result as IResultSet<AccessRule>));
  }

  public applyFilters(): void {
    this.page = 1;
    this.refresh();
  }

  public levelLabel(level: number | undefined): string {
    return ACCESS_LEVELS.find(x => x.value === level)?.label ?? `${level}`;
  }

  public openDeleteModal(item: AccessRule): void {
    const modal = this._modalService.show(AccessRuleDeleteModalComponent, {
      backdrop: 'static',
      keyboard: false,
      initialState: { id: item.id }
    });
    modal.onHidden?.subscribe(result => {
      if ((result as any)?.result) {
        this.refresh();
      }
    });
  }
}
