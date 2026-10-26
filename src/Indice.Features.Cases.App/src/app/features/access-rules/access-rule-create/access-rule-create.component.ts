import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, finalize, take } from 'rxjs';
import { LucideSave } from '@lucide/angular';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AddAccessRuleRequest, CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { ACCESS_LEVELS } from '../access-rules.component';

type TargetType = 'caseType' | 'checkpointType';
type MemberType = 'role' | 'group' | 'user';

@Component({
  selector: 'app-access-rule-create',
  imports: [RouterLink, TranslatePipe, PageHeaderComponent, LucideSave],
  template: `
    <app-page-header [title]="'accessRules.create.title' | translate" />

    <form class="panel max-w-3xl" novalidate (submit)="$event.preventDefault(); submit()">
      <div class="space-y-6 p-5 sm:p-6">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'accessRules.column.target' | translate }} <span class="text-error">*</span></legend>
          <div class="join w-full sm:w-auto">
            @for (option of targetTypes; track option.value) {
              <input type="radio" name="targetType" class="join-item btn btn-sm" [value]="option.value"
                [attr.aria-label]="option.label | translate" [checked]="targetType() === option.value" (change)="setTargetType(option.value)" />
            }
          </div>
          <select class="select mt-2 w-full" required [attr.aria-label]="'accessRules.column.target' | translate"
            [value]="targetId() ?? ''" (change)="targetId.set(value($event) || undefined)">
            <option value="" disabled>{{ 'accessRules.chooseTarget' | translate }}</option>
            @if (targetType() === 'caseType') {
              @for (caseType of caseTypes(); track caseType.id) {
                <option [value]="caseType.id">{{ caseType.title }} ({{ caseType.code }})</option>
              }
            } @else {
              @for (checkpointType of checkpointTypes(); track checkpointType.id) {
                <option [value]="checkpointType.id">{{ checkpointType.caseTypeCode }} / {{ checkpointType.code }}</option>
              }
            }
          </select>
        </fieldset>

        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'accessRules.column.member' | translate }} <span class="text-error">*</span></legend>
          <div class="join w-full sm:w-auto">
            @for (option of memberTypes; track option.value) {
              <input type="radio" name="memberType" class="join-item btn btn-sm" [value]="option.value"
                [attr.aria-label]="option.label | translate" [checked]="memberType() === option.value" (change)="memberType.set(option.value)" />
            }
          </div>
          <input type="text" class="input mt-2 w-full" required [attr.aria-label]="memberLabel() | translate"
            [placeholder]="memberLabel() | translate" [value]="memberValue()" (input)="memberValue.set(value($event))" />
        </fieldset>

        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'accessRules.column.accessLevel' | translate }} <span class="text-error">*</span></legend>
          <div class="grid gap-2 sm:grid-cols-3">
            @for (level of levels; track level.value) {
              <label class="flex cursor-pointer items-center gap-3 rounded-field border border-base-300 px-3 py-2.5 has-checked:border-primary has-checked:bg-primary/5">
                <input type="radio" name="accessLevel" class="radio radio-primary radio-sm" [checked]="accessLevel() === level.value" (change)="accessLevel.set(level.value)" />
                <span class="text-sm">{{ level.label | translate }}</span>
              </label>
            }
          </div>
        </fieldset>
      </div>
      <div class="flex justify-end gap-2 border-t border-base-300 px-5 py-3 sm:px-6">
        <a routerLink="/access-rules" class="btn btn-ghost">{{ 'common.cancel' | translate }}</a>
        <button type="submit" class="btn btn-primary" [disabled]="!canSave()">
          @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
          {{ 'common.save' | translate }}
        </button>
      </div>
    </form>
  `
})
export class AccessRuleCreateComponent implements OnInit {
  private readonly _router = inject(Router);
  private readonly _api = inject(CasesApiService);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _toaster = inject(TranslatedToasterService);

  protected readonly levels = ACCESS_LEVELS;
  protected readonly targetTypes: { value: TargetType; label: string }[] = [
    { value: 'caseType', label: 'accessRules.target.caseType' },
    { value: 'checkpointType', label: 'accessRules.target.checkpointType' }
  ];
  protected readonly memberTypes: { value: MemberType; label: string }[] = [
    { value: 'role', label: 'accessRules.column.role' },
    { value: 'group', label: 'accessRules.column.group' },
    { value: 'user', label: 'accessRules.column.user' }
  ];

  protected readonly targetType = signal<TargetType>('caseType');
  protected readonly memberType = signal<MemberType>('role');
  protected readonly targetId = signal<string | undefined>(undefined);
  protected readonly memberValue = signal('');
  protected readonly accessLevel = signal(0);
  protected readonly caseTypes = signal<CaseTypePartial[]>([]);
  protected readonly checkpointTypes = signal<CheckpointType[]>([]);
  protected readonly busy = signal(false);

  protected readonly memberLabel = computed(() => this.memberTypes.find(m => m.value === this.memberType())!.label);
  protected readonly canSave = computed(() => !this.busy() && !!this.targetId() && !!this.memberValue().trim());

  public ngOnInit(): void {
    this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe(result => this.caseTypes.set(result.items ?? []));
    this._caseTypeService.getCheckpointTypes().pipe(take(1)).subscribe(result => this.checkpointTypes.set(result));
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected setTargetType(type: TargetType): void {
    this.targetType.set(type);
    this.targetId.set(undefined);
  }

  protected submit(): void {
    if (!this.canSave()) {
      return;
    }
    const member = this.memberValue().trim();
    const request = new AddAccessRuleRequest({
      ruleCaseTypeId: this.targetType() === 'caseType' ? this.targetId() : undefined,
      ruleCheckpointTypeId: this.targetType() === 'checkpointType' ? this.targetId() : undefined,
      memberRole: this.memberType() === 'role' ? member : undefined,
      memberGroupId: this.memberType() === 'group' ? member : undefined,
      memberUserId: this.memberType() === 'user' ? member : undefined,
      accessLevel: this.accessLevel()
    });
    this.busy.set(true);
    this._api.createAccessRule(request).pipe(
      catchError(() => EMPTY),
      finalize(() => this.busy.set(false))
    ).subscribe(() => {
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.accessRuleCreated.body');
      this._router.navigate(['/access-rules']);
    });
  }
}
