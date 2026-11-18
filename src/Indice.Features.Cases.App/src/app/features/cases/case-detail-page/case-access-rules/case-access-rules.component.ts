import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideSearch, LucideShieldCheck } from '@lucide/angular';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { Case, CasesApiService } from 'src/app/core/services/cases-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { ACCESS_LEVELS } from '../../../access-rules/access-rules.component';

@Component({
  selector: 'app-case-access-rules',
  imports: [RouterLink, TranslatePipe, EmptyStateComponent, PaginationComponent, LucideSearch, LucideShieldCheck],
  template: `
    <form class="mb-3 grid gap-2 sm:grid-cols-2 lg:flex lg:flex-wrap lg:items-center" (submit)="$event.preventDefault(); applyFilters()">
      <input type="text" class="input input-sm lg:w-48" [placeholder]="'accessRules.column.role' | translate"
        [attr.aria-label]="'accessRules.column.role' | translate" [value]="roleFilter()" (input)="roleFilter.set(value($event))" />
      <input type="text" class="input input-sm lg:w-48" [placeholder]="'accessRules.column.group' | translate"
        [attr.aria-label]="'accessRules.column.group' | translate" [value]="groupFilter()" (input)="groupFilter.set(value($event))" />
      <select class="select select-sm lg:w-72" [attr.aria-label]="'accessRules.target.checkpointType' | translate"
        [value]="checkpointFilter()" (change)="checkpointFilter.set(value($event))">
        <option value="">{{ 'accessRules.allCheckpointTypes' | translate }}</option>
        @for (option of checkpointTypeOptions(); track option.id) {
          <option [value]="option.id" [selected]="option.id === checkpointFilter()">{{ option.caseTypeCode }} / {{ option.code }}</option>
        }
      </select>
      <div class="flex gap-2">
        <button type="submit" class="btn btn-sm"><svg lucideSearch class="size-4"></svg>{{ 'common.search' | translate }}</button>
        @if (hasFilters()) {
          <button type="button" class="btn btn-ghost btn-sm" (click)="clearFilters()">{{ 'filters.clearAll' | translate }}</button>
        }
      </div>
    </form>

    <div class="overflow-x-auto">
      <table class="table table-sm">
        <thead>
          <tr>
            <th>{{ 'accessRules.column.target' | translate }}</th>
            <th>{{ 'accessRules.column.member' | translate }}</th>
            <th>{{ 'accessRules.column.accessLevel' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @if (rules.error() || checkpointTypes.error()) {
            <tr>
              <td colspan="3">
                <app-empty-state [title]="'toasts.error.title' | translate" [message]="'toasts.error.body' | translate">
                  <button actions type="button" class="btn btn-sm" (click)="retry()">{{ 'common.retry' | translate }}</button>
                </app-empty-state>
              </td>
            </tr>
          } @else if (rules.isLoading() || checkpointTypes.isLoading()) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="3"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of items(); track item.id) {
              <tr class="hover:bg-base-200/60">
                <td>
                  @if (item.ruleCaseTypeId) {
                    <span class="text-base-content/60">{{ 'accessRules.target.caseType' | translate }}</span>
                    <span class="ml-1.5 font-mono text-xs">{{ case().caseType?.code || item.ruleCaseTypeId }}</span>
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
              </tr>
            } @empty {
              <tr>
                <td colspan="3">
                  <app-empty-state [title]="'accessRules.empty.title' | translate" [message]="'accessRules.empty.message' | translate">
                    <svg icon lucideShieldCheck class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
    @if (filteredRules().length > 0 && !rules.error() && !checkpointTypes.error()) {
      <app-pagination class="border-t border-base-300 px-4 py-3" [page]="page()" [pageSize]="pageSize()" [count]="filteredRules().length"
        (pageChange)="requestedPage.set($event)" (pageSizeChange)="setPageSize($event)" />
    }
  `
})
export class CaseAccessRulesComponent {
  public readonly case = input.required<Case>();

  private readonly _api = inject(CasesApiService);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _filters = signal({ role: '', group: '', checkpoint: '' });

  protected readonly rules = rxResource({
    params: () => this.case().id,
    stream: ({ params }) => this._api.getCaseAccessRules(params)
  });
  protected readonly checkpointTypes = rxResource({
    stream: () => this._caseTypeService.getCheckpointTypes()
  });
  protected readonly checkpointTypeOptions = computed(() => (this.checkpointTypes.hasValue() ? this.checkpointTypes.value() : [])
    .filter(checkpoint => checkpoint.caseTypeCode === this.case().caseType?.code));
  protected readonly checkpointTypeCodes = computed(() => Object.fromEntries(
    this.checkpointTypeOptions().map(checkpoint => [checkpoint.id!, `${checkpoint.caseTypeCode} / ${checkpoint.code}`])
  ));
  protected readonly roleFilter = signal('');
  protected readonly groupFilter = signal('');
  protected readonly checkpointFilter = signal('');
  protected readonly hasFilters = computed(() => {
    const { role, group, checkpoint } = this._filters();
    return !!(role || group || checkpoint);
  });
  protected readonly filteredRules = computed(() => {
    const { role, group, checkpoint } = this._filters();
    return (this.rules.hasValue() ? this.rules.value() : []).filter(rule => (!role || rule.memberRole === role)
      && (!group || rule.memberGroupId === group)
      && (!checkpoint || rule.ruleCheckpointTypeId === checkpoint));
  });
  protected readonly requestedPage = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly page = computed(() => Math.min(this.requestedPage(), Math.max(1, Math.ceil(this.filteredRules().length / this.pageSize()))));
  protected readonly items = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filteredRules().slice(start, start + this.pageSize());
  });

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected applyFilters(): void {
    this._filters.set({ role: this.roleFilter().trim(), group: this.groupFilter().trim(), checkpoint: this.checkpointFilter() });
    this.requestedPage.set(1);
  }

  protected clearFilters(): void {
    this.roleFilter.set('');
    this.groupFilter.set('');
    this.checkpointFilter.set('');
    this.applyFilters();
  }

  protected setPageSize(size: number): void {
    this.pageSize.set(size);
    this.requestedPage.set(1);
  }

  protected levelLabel(level: number | undefined): string {
    return ACCESS_LEVELS.find(option => option.value === level)?.label ?? `${level}`;
  }

  protected levelClass(level: number | undefined): string {
    return level === 2 ? 'badge-secondary' : level === 1 ? 'badge-primary badge-soft' : 'badge-ghost';
  }

  protected retry(): void {
    this.rules.reload();
    this.checkpointTypes.reload();
  }
}
