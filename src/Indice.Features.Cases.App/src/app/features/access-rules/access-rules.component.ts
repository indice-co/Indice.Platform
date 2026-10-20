import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, filter, forkJoin, map, switchMap, take } from 'rxjs';
import { LucidePlus, LucideSearch, LucideShieldCheck, LucideTrash } from '@lucide/angular';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AccessRule, CasesApiService } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

export const ACCESS_LEVELS: { value: number; label: string }[] = [
  { value: 0, label: 'accessRules.level.read' },
  { value: 1, label: 'accessRules.level.manage' },
  { value: 2, label: 'accessRules.level.administer' }
];

/** Access rule list. Filters live in the URL as `role`, `group` and `checkpoint` query params. */
@Component({
  selector: 'app-access-rules',
  imports: [RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, LucidePlus, LucideSearch, LucideShieldCheck, LucideTrash],
  template: `
    <app-page-header [title]="'accessRules.title' | translate" [subtitle]="'accessRules.subtitle' | translate">
      <div actions>
        <a routerLink="/access-rules/create" class="btn btn-primary btn-sm">
          <svg lucidePlus class="size-4"></svg>{{ 'accessRules.create.title' | translate }}
        </a>
      </div>
    </app-page-header>

    <form class="mb-3 grid gap-2 sm:grid-cols-2 lg:flex lg:items-center" (submit)="$event.preventDefault(); applyFilters()">
      <input type="text" class="input input-sm lg:w-48" [placeholder]="'accessRules.column.role' | translate"
        [attr.aria-label]="'accessRules.column.role' | translate" [value]="roleFilter()" (input)="roleFilter.set(value($event))" />
      <input type="text" class="input input-sm lg:w-48" [placeholder]="'accessRules.column.group' | translate"
        [attr.aria-label]="'accessRules.column.group' | translate" [value]="groupFilter()" (input)="groupFilter.set(value($event))" />
      <select class="select select-sm lg:w-72" [attr.aria-label]="'accessRules.target.checkpointType' | translate"
        [value]="checkpointFilter()" (change)="checkpointFilter.set(value($event))">
        <option value="">{{ 'accessRules.allCheckpointTypes' | translate }}</option>
        @for (option of checkpointTypeOptions(); track option.id) {
          <option [value]="option.id" [selected]="option.id === checkpointFilter()">{{ option.label }}</option>
        }
      </select>
      <div class="flex gap-2">
        <button type="submit" class="btn btn-sm"><svg lucideSearch class="size-4"></svg>{{ 'common.search' | translate }}</button>
        @if (hasFilters()) {
          <button type="button" class="btn btn-ghost btn-sm" (click)="clearFilters()">{{ 'filters.clearAll' | translate }}</button>
        }
      </div>
    </form>

    <div class="panel overflow-x-auto">
      <table class="table table-sm">
        <thead>
          <tr>
            <th>{{ 'accessRules.column.target' | translate }}</th>
            <th>{{ 'accessRules.column.member' | translate }}</th>
            <th>{{ 'accessRules.column.accessLevel' | translate }}</th>
            <th class="w-16 text-right"><span class="sr-only">{{ 'accessRules.column.actions' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="4"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  @if (item.ruleCaseTypeId) {
                    <span class="text-base-content/60">{{ 'accessRules.target.caseType' | translate }}</span>
                    <span class="ml-1.5 font-mono text-xs">{{ caseTypeCodes()[item.ruleCaseTypeId] || item.ruleCaseTypeId }}</span>
                  }
                  @if (item.ruleCheckpointTypeId) {
                    <span class="text-base-content/60">{{ 'accessRules.target.checkpointType' | translate }}</span>
                    <span class="ml-1.5 font-mono text-xs">{{ checkpointTypeCodes()[item.ruleCheckpointTypeId] || item.ruleCheckpointTypeId }}</span>
                  }
                  @if (item.ruleCaseId) {
                    <div>
                      <span class="text-base-content/60">{{ 'accessRules.target.case' | translate }}</span>
                      <a class="link link-hover ml-1.5 font-mono text-xs" [routerLink]="['/cases', item.ruleCaseId, 'details']">{{ item.ruleCaseId }}</a>
                    </div>
                  }
                </td>
                <td>
                  @if (item.memberRole) { <span class="text-base-content/60">{{ 'accessRules.column.role' | translate }}</span> <span class="ml-1.5">{{ item.memberRole }}</span> }
                  @if (item.memberGroupId) { <span class="text-base-content/60">{{ 'accessRules.column.group' | translate }}</span> <span class="ml-1.5">{{ item.memberGroupId }}</span> }
                  @if (item.memberUserId) { <span class="text-base-content/60">{{ 'accessRules.column.user' | translate }}</span> <span class="ml-1.5 font-mono text-xs">{{ item.memberUserId }}</span> }
                </td>
                <td><span class="badge badge-sm" [class]="levelClass(item.accessLevel)">{{ levelLabel(item.accessLevel) | translate }}</span></td>
                <td class="text-right">
                  <span class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="delete(item)" [attr.aria-label]="'common.delete' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4">
                  <app-empty-state [title]="'accessRules.empty.title' | translate" [message]="'accessRules.empty.message' | translate">
                    <svg icon lucideShieldCheck class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
      @if (list.count() > 0) {
        <app-pagination class="border-t border-base-300 px-4 py-3" [page]="list.page()" [pageSize]="list.pageSize()" [count]="list.count()"
          (pageChange)="list.setPage($event)" (pageSizeChange)="list.setPageSize($event)" />
      }
    </div>
  `
})
export class AccessRulesComponent implements OnInit {
  private readonly _api = inject(CasesApiService);
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(TranslatedToasterService);

  protected readonly caseTypeCodes = signal<Record<string, string>>({});
  protected readonly checkpointTypeCodes = signal<Record<string, string>>({});
  protected readonly checkpointTypeOptions = signal<{ id: string; label: string }[]>([]);
  protected readonly roleFilter = signal('');
  protected readonly groupFilter = signal('');
  protected readonly checkpointFilter = signal('');
  protected readonly hasFilters = signal(false);

  protected readonly list = new ListState<AccessRule>({
    pageSize: 20,
    load: query => {
      const params = this._route.snapshot.queryParamMap;
      const role = params.get('role') ?? '';
      const group = params.get('group') ?? '';
      const checkpoint = params.get('checkpoint') ?? '';
      this.roleFilter.set(role);
      this.groupFilter.set(group);
      this.checkpointFilter.set(checkpoint);
      this.hasFilters.set(!!(role || group || checkpoint));
      return this._api.getAccessRules(
        query.page,
        query.pageSize,
        undefined,
        undefined,
        role ? `role::eq::${role}` : undefined,
        group ? `groupId::eq::${group}` : undefined,
        checkpoint ? `checkpoint::eq::${checkpoint}` : undefined,
        undefined
      ).pipe(map(result => ({ count: result.count, items: result.items })));
    }
  });

  public ngOnInit(): void {
    forkJoin({
      caseTypes: this._caseTypeService.getCaseTypes().pipe(take(1)),
      checkpointTypes: this._caseTypeService.getCheckpointTypes().pipe(take(1))
    }).subscribe(({ caseTypes, checkpointTypes }) => {
      this.caseTypeCodes.set(Object.fromEntries((caseTypes.items ?? []).map(x => [x.id!, x.code!])));
      const checkpoints = checkpointTypes.map(x => ({ id: x.id!, label: `${x.caseTypeCode} / ${x.code}` }));
      this.checkpointTypeCodes.set(Object.fromEntries(checkpoints.map(x => [x.id, x.label])));
      this.checkpointTypeOptions.set(checkpoints);
    });
    this.list.start();
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected applyFilters(): void {
    this.navigateFilters(this.roleFilter().trim(), this.groupFilter().trim(), this.checkpointFilter());
  }

  protected clearFilters(): void {
    this.navigateFilters('', '', '');
  }

  protected levelLabel(level: number | undefined): string {
    return ACCESS_LEVELS.find(x => x.value === level)?.label ?? `${level}`;
  }

  protected levelClass(level: number | undefined): string {
    return level === 2 ? 'badge-secondary' : level === 1 ? 'badge-primary badge-soft' : 'badge-ghost';
  }

  protected delete(item: AccessRule): void {
    this._dialog.confirm({
      title: 'accessRules.delete.title',
      message: 'accessRules.delete.confirm',
      confirmLabel: 'common.delete',
      tone: 'danger'
    }).pipe(
      filter(Boolean),
      switchMap(() => this._api.deleteAccessRule(item.id!)),
      catchError(err => {
        this._toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        return EMPTY;
      })
    ).subscribe(() => {
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.accessRuleDeleted.body');
      this.list.refresh();
    });
  }

  private navigateFilters(role: string, group: string, checkpoint: string): void {
    const params = this._route.snapshot.queryParamMap;
    const unchanged = (params.get('role') ?? '') === role && (params.get('group') ?? '') === group
      && (params.get('checkpoint') ?? '') === checkpoint && this.list.page() === 1;
    if (unchanged) {
      // same URL does not re-emit query params, so reload explicitly
      this.list.refresh();
      return;
    }
    this._router.navigate([], {
      relativeTo: this._route,
      queryParams: { role: role || null, group: group || null, checkpoint: checkpoint || null, page: 1 },
      queryParamsHandling: 'merge'
    });
  }
}
