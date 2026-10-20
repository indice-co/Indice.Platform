import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, forkJoin, Observable } from 'rxjs';
import { catchError, filter, map, switchMap, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AccessRule, CasesApiService, CaseTypePartial } from 'src/app/core/services/cases-api.service';
import { ConfirmDialogComponent, ConfirmDialogData } from 'src/app/ui/feedback/confirm-dialog.component';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { IResultSet } from 'src/app/ui/list/filtering';
import { ListPage } from 'src/app/ui/list/list-page';
import { PagerComponent } from 'src/app/ui/list/pager.component';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';

export const ACCESS_LEVELS: { value: number; label: string }[] = [
  { value: 0, label: 'accessRules.level.read' },
  { value: 1, label: 'accessRules.level.manage' },
  { value: 2, label: 'accessRules.level.administer' }
];

@Component({
  selector: 'app-access-rules',
  imports: [FormsModule, RouterLink, NgIcon, TranslatePipe, PageHeaderComponent, PagerComponent, EmptyStateComponent],
  template: `
    <div class="page">
      <app-page-header [title]="'accessRules.title' | translate">
        <a actions routerLink="/access-rules/create" class="btn btn-primary">
          <ng-icon name="lucidePlus" size="1rem" /> {{ 'accessRules.create.action' | translate }}
        </a>
      </app-page-header>

      <section class="panel overflow-hidden">
        <form class="flex flex-wrap items-end gap-2 border-b border-base-300 px-4 py-3" (ngSubmit)="applyFilters()">
          <label class="w-full sm:w-48">
            <span class="mb-1.5 block text-[0.8125rem] text-quiet">{{ 'accessRules.column.role' | translate }}</span>
            <input type="text" class="input input-sm w-full" name="roleFilter" [(ngModel)]="roleFilter" />
          </label>
          <label class="w-full sm:w-48">
            <span class="mb-1.5 block text-[0.8125rem] text-quiet">{{ 'accessRules.column.group' | translate }}</span>
            <input type="text" class="input input-sm w-full" name="groupFilter" [(ngModel)]="groupFilter" />
          </label>
          <label class="w-full sm:w-64">
            <span class="mb-1.5 block text-[0.8125rem] text-quiet">{{ 'accessRules.target.checkpointType' | translate }}</span>
            <select class="select select-sm w-full" name="checkpointTypeFilter" [(ngModel)]="checkpointTypeFilter">
              <option [ngValue]="undefined">{{ 'accessRules.allCheckpointTypes' | translate }}</option>
              @for (option of checkpointTypeOptions(); track option.id) {
                <option [ngValue]="option.id">{{ option.label }}</option>
              }
            </select>
          </label>
          <button type="submit" class="btn btn-sm">
            <ng-icon name="lucideSearch" size="0.95rem" /> {{ 'common.search' | translate }}
          </button>
        </form>

        @if (items(); as rows) {
          @if (rows.length === 0) {
            <app-empty-state icon="lucideShieldCheck" [title]="'accessRules.empty.title' | translate" [message]="'accessRules.empty.message' | translate">
              <a routerLink="/access-rules/create" class="btn btn-primary btn-sm"><ng-icon name="lucidePlus" size="1rem" /> {{ 'accessRules.create.action' | translate }}</a>
            </app-empty-state>
          } @else {
            <div class="overflow-x-auto transition-opacity" [class.opacity-60]="loading()">
              <table class="table data-table">
                <thead>
                  <tr>
                    <th>{{ 'accessRules.column.target' | translate }}</th>
                    <th>{{ 'accessRules.column.member' | translate }}</th>
                    <th>{{ 'accessRules.column.accessLevel' | translate }}</th>
                    <th class="w-px"><span class="sr-only">{{ 'accessRules.column.actions' | translate }}</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (item of rows; track item.id) {
                    <tr>
                      <td>
                        <div class="flex flex-col gap-0.5">
                          @if (item.ruleCaseTypeId) {
                            <span><span class="text-quiet">{{ 'accessRules.target.caseType' | translate }}:&nbsp;</span><span class="ref">{{ caseTypeCodes[item.ruleCaseTypeId] || item.ruleCaseTypeId }}</span></span>
                          }
                          @if (item.ruleCheckpointTypeId) {
                            <span><span class="text-quiet">{{ 'accessRules.target.checkpointType' | translate }}:&nbsp;</span><span class="ref">{{ checkpointTypeCodes[item.ruleCheckpointTypeId] || item.ruleCheckpointTypeId }}</span></span>
                          }
                          @if (item.ruleCaseId) {
                            <span><span class="text-quiet">{{ 'accessRules.target.case' | translate }}:&nbsp;</span><span class="ref">{{ item.ruleCaseId }}</span></span>
                          }
                        </div>
                      </td>
                      <td>
                        <div class="flex flex-col gap-0.5">
                          @if (item.memberRole) {
                            <span><span class="text-quiet">{{ 'accessRules.column.role' | translate }}:&nbsp;</span><span class="font-medium">{{ item.memberRole }}</span></span>
                          }
                          @if (item.memberGroupId) {
                            <span><span class="text-quiet">{{ 'accessRules.column.group' | translate }}:&nbsp;</span><span class="ref">{{ item.memberGroupId }}</span></span>
                          }
                          @if (item.memberUserId) {
                            <span><span class="text-quiet">{{ 'accessRules.column.user' | translate }}:&nbsp;</span><span class="ref">{{ item.memberUserId }}</span></span>
                          }
                        </div>
                      </td>
                      <td><span class="badge badge-soft badge-primary">{{ levelLabel(item.accessLevel) | translate }}</span></td>
                      <td class="text-right">
                        <div class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                          <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="openDeleteModal(item)"
                                  [attr.aria-label]="'accessRules.delete.title' | translate">
                            <ng-icon name="lucideTrash2" size="1rem" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            <div class="border-t border-base-300 px-4 py-3">
              <app-pager [page]="page()" [pageSize]="pageSize()" [count]="count()" (pageChange)="pageChanged($event)" (pageSizeChange)="pageSizeChanged($event)" />
            </div>
          }
        } @else {
          <div class="space-y-3 p-5" aria-busy="true">
            @for (row of [1, 2, 3, 4, 5]; track row) {
              <div class="skeleton h-8 w-full"></div>
            }
          </div>
        }
      </section>
    </div>
  `
})
export class AccessRulesComponent extends ListPage<AccessRule> implements OnInit {
  private readonly api = inject(CasesApiService);
  private readonly caseTypeService = inject(CaseTypeService);
  private readonly dialogs = inject(DialogService);
  private readonly toaster = inject(ToastService);

  protected caseTypeCodes: { [id: string]: string } = {};
  protected checkpointTypeCodes: { [id: string]: string } = {};
  protected readonly checkpointTypeOptions = signal<{ id: string; label: string }[]>([]);
  protected roleFilter: string | undefined;
  protected groupFilter: string | undefined;
  protected checkpointTypeFilter: string | undefined;

  constructor() {
    super();
    this.pageSize.set(20);
  }

  public ngOnInit(): void {
    forkJoin({
      caseTypes: this.caseTypeService.getCaseTypes().pipe(take(1)) as Observable<{ items?: CaseTypePartial[] }>,
      checkpointTypes: this.caseTypeService.getCheckpointTypes().pipe(take(1))
    }).subscribe(({ caseTypes, checkpointTypes }) => {
      (caseTypes.items ?? []).forEach(x => this.caseTypeCodes[x.id!] = x.code!);
      checkpointTypes.forEach(x => this.checkpointTypeCodes[x.id!] = `${x.caseTypeCode} / ${x.code}`);
      this.checkpointTypeOptions.set(checkpointTypes.map(x => ({ id: x.id!, label: `${x.caseTypeCode} / ${x.code}` })));
      this.startList();
    });
  }

  public loadItems(): Observable<IResultSet<AccessRule> | null | undefined> {
    return this.api.getAccessRules(
      this.page(),
      this.pageSize(),
      undefined,
      undefined,
      this.roleFilter ? `role::eq::${this.roleFilter}` : undefined,
      this.groupFilter ? `groupId::eq::${this.groupFilter}` : undefined,
      this.checkpointTypeFilter ? `checkpoint::eq::${this.checkpointTypeFilter}` : undefined,
      undefined
    ).pipe(map(result => result as IResultSet<AccessRule>));
  }

  protected applyFilters(): void {
    this.refresh();
  }

  protected levelLabel(level: number | undefined): string {
    return ACCESS_LEVELS.find(x => x.value === level)?.label ?? `${level}`;
  }

  protected openDeleteModal(item: AccessRule): void {
    const data: ConfirmDialogData = {
      title: 'accessRules.delete.title',
      message: 'accessRules.delete.confirm',
      confirmLabel: 'common.delete',
      cancelLabel: 'common.return',
      tone: 'danger'
    };
    this.dialogs.open<ConfirmDialogComponent, boolean>(ConfirmDialogComponent, { data }).closed
      .pipe(
        filter(confirmed => confirmed === true),
        switchMap(() => this.api.deleteAccessRule(item.id!).pipe(
          catchError(err => {
            this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
            return EMPTY;
          })
        ))
      )
      .subscribe(() => {
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.accessRuleDeleted.body');
        this.refresh();
      });
  }
}
