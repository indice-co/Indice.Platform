import { Component, OnInit, inject, input, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, filter, map, switchMap, take } from 'rxjs';
import { LucideCheck, LucideMilestone, LucideMinus, LucidePencil, LucidePlus, LucideTrash } from '@lucide/angular';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

@Component({
  selector: 'app-checkpoint-types',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    LucideCheck, LucideMilestone, LucideMinus, LucidePencil, LucidePlus, LucideTrash
  ],
  template: `
    @if (!embedded()) {
      <app-page-header [title]="'checkpointTypes.title' | translate" [subtitle]="'checkpointTypes.subtitle' | translate">
        <div actions>
          <a routerLink="/checkpoint-types/create" [queryParams]="createQueryParams()" class="btn btn-primary btn-sm">
            <svg lucidePlus class="size-4"></svg>{{ 'checkpointTypes.create.title' | translate }}
          </a>
        </div>
      </app-page-header>
    }

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      @if (!scopeCaseTypeId()) {
        <select class="select select-sm sm:w-72" [attr.aria-label]="'checkpointTypes.column.caseType' | translate"
          [value]="selectedCaseTypeId() ?? ''" (change)="caseTypeChanged($any($event.target).value)">
          <option value="">{{ 'checkpointTypes.allCaseTypes' | translate }}</option>
          @for (caseType of caseTypes(); track caseType.id) {
            <option [value]="caseType.id" [selected]="caseType.id === selectedCaseTypeId()">{{ caseType.title }} ({{ caseType.code }})</option>
          }
        </select>
      }
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'common.search' | translate" (search)="list.setSearch($event)" />
      @if (embedded()) {
        <a routerLink="/checkpoint-types/create" [queryParams]="createQueryParams()" class="btn btn-sm sm:ml-auto">
          <svg lucidePlus class="size-4"></svg>{{ 'checkpointTypes.create.title' | translate }}
        </a>
      }
    </div>

    <div class="panel overflow-x-auto">
      <table class="table table-sm">
        <thead>
          <tr>
            @if (!scopeCaseTypeId()) { <th>{{ 'checkpointTypes.column.caseType' | translate }}</th> }
            <th>{{ 'checkpointTypes.column.code' | translate }}</th>
            <th>{{ 'checkpointTypes.column.title' | translate }}</th>
            <th>{{ 'checkpointTypes.column.status' | translate }}</th>
            <th class="text-center">{{ 'checkpointTypes.column.private' | translate }}</th>
            <th class="w-24 text-right"><span class="sr-only">{{ 'checkpointTypes.column.actions' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                @if (!scopeCaseTypeId()) { <td><span class="font-mono text-xs">{{ item.caseTypeCode }}</span></td> }
                <td><span class="font-mono text-xs">{{ item.code }}</span></td>
                <td>
                  <span class="font-medium">{{ item.title }}</span>
                  @if (item.description) {
                    <p class="max-w-md truncate text-xs text-base-content/60">{{ item.description }}</p>
                  }
                </td>
                <td>
                  @if (item.status) {
                    <span class="stamp" [class]="'stamp-' + item.status.toLowerCase()">{{ item.status }}</span>
                  }
                </td>
                <td class="text-center">
                  @if (item.private) {
                    <svg lucideCheck class="inline size-4 text-base-content/80" [attr.aria-label]="'common.yes' | translate"></svg>
                  } @else {
                    <svg lucideMinus class="inline size-4 text-base-content/30" [attr.aria-label]="'common.no' | translate"></svg>
                  }
                </td>
                <td class="text-right whitespace-nowrap">
                  <span class="tooltip" [attr.data-tip]="'common.edit' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/checkpoint-types', item.id, 'edit']" [queryParams]="editQueryParams()"
                      [attr.aria-label]="'common.edit' | translate">
                      <svg lucidePencil class="size-4"></svg>
                    </a>
                  </span>
                  <span class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="delete(item)" [attr.aria-label]="'common.delete' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="'checkpointTypes.empty.title' | translate" [message]="'checkpointTypes.empty.message' | translate">
                    <svg icon lucideMilestone class="size-5"></svg>
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
export class CheckpointTypesComponent implements OnInit {
  private readonly _api = inject(CasesApiService);
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(TranslatedToasterService);

  /** When set, the list is scoped to this case type (embedded in the case type edit page). */
  public readonly scopeCaseTypeId = input<string | undefined>(undefined);
  /** Hides the page header when embedded in another page. */
  public readonly embedded = input(false);
  /** App URL the checkpoint type create / edit pages return to, instead of this list. */
  public readonly returnUrl = input<string>();

  protected readonly caseTypes = signal<CaseTypePartial[]>([]);
  protected readonly selectedCaseTypeId = signal<string | undefined>(undefined);

  protected readonly list = new ListState<CheckpointType>({
    pageSize: 20,
    sort: 'caseTypeCode',
    dir: 'asc',
    load: query => {
      const caseTypeId = this.scopeCaseTypeId() ?? this._route.snapshot.queryParamMap.get('caseTypeId') ?? undefined;
      this.selectedCaseTypeId.set(caseTypeId);
      return this._api.getCheckpointTypesList(
        query.page,
        query.pageSize,
        query.sort ? (query.dir === 'desc' ? `${query.sort}-` : query.sort) : undefined,
        query.search || undefined,
        caseTypeId
      ).pipe(map(result => ({ count: result.count, items: result.items })));
    }
  });

  public ngOnInit(): void {
    if (!this.scopeCaseTypeId()) {
      this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe(result => this.caseTypes.set(result.items ?? []));
    }
    this.list.start();
  }

  protected caseTypeChanged(caseTypeId: string): void {
    this._router.navigate([], {
      relativeTo: this._route,
      queryParams: { caseTypeId: caseTypeId || null, page: 1 },
      queryParamsHandling: 'merge'
    });
  }

  protected createQueryParams(): Record<string, string> {
    const caseTypeId = this.selectedCaseTypeId();
    return { ...(caseTypeId ? { caseTypeId } : {}), ...this.editQueryParams() };
  }

  protected editQueryParams(): Record<string, string> {
    const returnUrl = this.returnUrl();
    return returnUrl ? { returnUrl } : {};
  }

  protected delete(item: CheckpointType): void {
    this._dialog.confirm({
      title: 'checkpointTypes.delete.title',
      message: 'checkpointTypes.delete.confirmCode',
      messageParams: { code: item.code },
      confirmLabel: 'common.delete',
      tone: 'danger'
    }).pipe(
      filter(Boolean),
      switchMap(() => this._api.deleteCheckpointType(item.id!)),
      catchError(() => EMPTY)
    ).subscribe(() => {
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.checkpointTypeDeleted.body');
      this.list.refresh();
    });
  }
}
