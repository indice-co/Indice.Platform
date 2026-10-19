import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { ToastType } from '@indice/ng-components';
import { EMPTY } from 'rxjs';
import { catchError, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AddAccessRuleRequest, CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { ACCESS_LEVELS } from '../access-rules.component';

@Component({
    selector: 'app-access-rule-create',
    templateUrl: './access-rule-create.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class AccessRuleCreateComponent implements OnInit {
  public targetType: 'caseType' | 'checkpointType' = 'caseType';
  public memberType: 'role' | 'group' | 'user' = 'role';
  public targetId: string | undefined;
  public memberValue = '';
  public accessLevel = 0;
  public levels = ACCESS_LEVELS;
  public caseTypes: CaseTypePartial[] = [];
  public checkpointTypes: CheckpointType[] = [];
  public busy = false;

  constructor(
    private _router: Router,
    private _api: CasesApiService,
    private _caseTypeService: CaseTypeService,
    private _toaster: TranslatedToasterService) { }

  ngOnInit(): void {
    this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe(result => this.caseTypes = result.items ?? []);
    this._api.getCheckpointTypesList(1, 1000, 'caseTypeCode').subscribe(result => this.checkpointTypes = result.items ?? []);
  }

  public submit(): void {
    const request = new AddAccessRuleRequest({
      ruleCaseTypeId: this.targetType === 'caseType' ? this.targetId : undefined,
      ruleCheckpointTypeId: this.targetType === 'checkpointType' ? this.targetId : undefined,
      memberRole: this.memberType === 'role' ? this.memberValue : undefined,
      memberGroupId: this.memberType === 'group' ? this.memberValue : undefined,
      memberUserId: this.memberType === 'user' ? this.memberValue : undefined,
      accessLevel: this.accessLevel
    });
    this.busy = true;
    this._api.createAccessRule(request).pipe(
      catchError(err => {
        this.busy = false;
        this._toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        return EMPTY;
      })
    ).subscribe(_ => {
      this.busy = false;
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.accessRuleCreated.body');
      this.goBack();
    });
  }

  public goBack(): void {
    this._router.navigate(['/access-rules']);
  }
}
