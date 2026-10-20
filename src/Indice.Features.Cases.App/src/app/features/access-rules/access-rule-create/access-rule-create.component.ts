import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY } from 'rxjs';
import { catchError, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AddAccessRuleRequest, CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { ACCESS_LEVELS } from '../access-rules.component';

/** Grants a role, group or user an access level on a case type or a checkpoint type. */
@Component({
  selector: 'app-access-rule-create',
  imports: [FormsModule, NgIcon, TranslatePipe, PageHeaderComponent],
  template: `
    <div class="page max-w-3xl">
      <app-page-header [title]="'accessRules.create.title' | translate">
        <button eyebrow type="button" class="link link-hover mb-1 inline-flex items-center gap-1 text-[0.8125rem] text-quiet" (click)="goBack()">
          <ng-icon name="lucideArrowLeft" size="0.9rem" /> {{ 'accessRules.title' | translate }}
        </button>
      </app-page-header>

      <form #form="ngForm" class="panel" [class.opacity-60]="busy()" (ngSubmit)="form.valid && submit()">
        <div class="panel-body space-y-6">
          <fieldset>
            <legend class="mb-2 text-sm font-medium">{{ 'accessRules.column.target' | translate }} <span class="text-error">*</span></legend>
            <div class="grid gap-2 sm:grid-cols-[minmax(0,14rem)_minmax(0,1fr)]">
              <select id="targetType" name="targetType" class="select w-full" [attr.aria-label]="'accessRules.column.target' | translate"
                      [(ngModel)]="targetType" (ngModelChange)="targetId = undefined">
                <option value="caseType">{{ 'accessRules.target.caseType' | translate }}</option>
                <option value="checkpointType">{{ 'accessRules.target.checkpointType' | translate }}</option>
              </select>
              <select id="targetId" name="targetId" class="select w-full" required
                      [attr.aria-label]="(targetType === 'caseType' ? 'accessRules.target.caseType' : 'accessRules.target.checkpointType') | translate"
                      [(ngModel)]="targetId">
                @if (targetType === 'caseType') {
                  @for (caseType of caseTypes(); track caseType.id) {
                    <option [ngValue]="caseType.id">{{ caseType.title }} ({{ caseType.code }})</option>
                  }
                } @else {
                  @for (checkpointType of checkpointTypes(); track checkpointType.id) {
                    <option [ngValue]="checkpointType.id">{{ checkpointType.caseTypeCode }} / {{ checkpointType.code }}</option>
                  }
                }
              </select>
            </div>
          </fieldset>

          <fieldset>
            <legend class="mb-2 text-sm font-medium">{{ 'accessRules.column.member' | translate }} <span class="text-error">*</span></legend>
            <div class="grid gap-2 sm:grid-cols-[minmax(0,14rem)_minmax(0,1fr)]">
              <select id="memberType" name="memberType" class="select w-full" [attr.aria-label]="'accessRules.column.member' | translate" [(ngModel)]="memberType">
                <option value="role">{{ 'accessRules.column.role' | translate }}</option>
                <option value="group">{{ 'accessRules.column.group' | translate }}</option>
                <option value="user">{{ 'accessRules.column.user' | translate }}</option>
              </select>
              <input id="memberValue" name="memberValue" type="text" class="input w-full" required
                     [attr.aria-label]="(memberType === 'role' ? 'accessRules.column.role' : memberType === 'group' ? 'accessRules.column.group' : 'accessRules.column.user') | translate"
                     [(ngModel)]="memberValue" />
            </div>
          </fieldset>

          <fieldset>
            <legend class="mb-2 text-sm font-medium">{{ 'accessRules.column.accessLevel' | translate }} <span class="text-error">*</span></legend>
            <div class="join" role="radiogroup">
              @for (level of levels; track level.value) {
                <input class="join-item btn" type="radio" name="accessLevel" [value]="level.value"
                       [attr.aria-label]="level.label | translate" [checked]="accessLevel === level.value" (change)="accessLevel = level.value" />
              }
            </div>
          </fieldset>
        </div>
        <div class="flex justify-end gap-2 border-t border-base-300 px-5 py-3">
          <button type="button" class="btn btn-ghost" (click)="goBack()">{{ 'common.return' | translate }}</button>
          <button type="submit" class="btn btn-primary" [disabled]="busy() || form.invalid">
            <ng-icon name="lucideSave" size="1rem" /> {{ 'common.save' | translate }}
          </button>
        </div>
      </form>
    </div>
  `
})
export class AccessRuleCreateComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly api = inject(CasesApiService);
  private readonly caseTypeService = inject(CaseTypeService);
  private readonly toaster = inject(ToastService);

  protected targetType: 'caseType' | 'checkpointType' = 'caseType';
  protected memberType: 'role' | 'group' | 'user' = 'role';
  protected targetId: string | undefined;
  protected memberValue = '';
  protected accessLevel = 0;
  protected readonly levels = ACCESS_LEVELS;
  protected readonly caseTypes = signal<CaseTypePartial[]>([]);
  protected readonly checkpointTypes = signal<CheckpointType[]>([]);
  protected readonly busy = signal(false);

  public ngOnInit(): void {
    this.caseTypeService.getCaseTypes().pipe(take(1)).subscribe((result: { items?: CaseTypePartial[] }) => this.caseTypes.set(result.items ?? []));
    this.caseTypeService.getCheckpointTypes().pipe(take(1)).subscribe(result => this.checkpointTypes.set(result));
  }

  protected submit(): void {
    const request = new AddAccessRuleRequest({
      ruleCaseTypeId: this.targetType === 'caseType' ? this.targetId : undefined,
      ruleCheckpointTypeId: this.targetType === 'checkpointType' ? this.targetId : undefined,
      memberRole: this.memberType === 'role' ? this.memberValue : undefined,
      memberGroupId: this.memberType === 'group' ? this.memberValue : undefined,
      memberUserId: this.memberType === 'user' ? this.memberValue : undefined,
      accessLevel: this.accessLevel
    });
    this.busy.set(true);
    this.api.createAccessRule(request).pipe(
      catchError(err => {
        this.busy.set(false);
        this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        return EMPTY;
      })
    ).subscribe(_ => {
      this.busy.set(false);
      this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.accessRuleCreated.body');
      this.goBack();
    });
  }

  protected goBack(): void {
    this.router.navigate(['/access-rules']);
  }
}
