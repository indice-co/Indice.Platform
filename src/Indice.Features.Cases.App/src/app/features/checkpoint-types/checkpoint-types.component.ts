import { NgTemplateOutlet } from '@angular/common';
import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable } from 'rxjs';
import { catchError, filter, map, switchMap, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial, CheckpointType } from 'src/app/core/services/cases-api.service';
import { ConfirmDialogComponent, ConfirmDialogData } from 'src/app/ui/feedback/confirm-dialog.component';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { IResultSet } from 'src/app/ui/list/filtering';
import { ListPage } from 'src/app/ui/list/list-page';
import { PagerComponent } from 'src/app/ui/list/pager.component';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';
import { StatusMarkComponent } from 'src/app/ui/status/case-status';

/**
 * Checkpoint types, optionally scoped to one case type. With `embedded` it renders without the page frame, inside the
 * case-type edit page.
 */
@Component({
  selector: 'app-checkpoint-types',
  imports: [NgTemplateOutlet, FormsModule, RouterLink, NgIcon, TranslatePipe, PageHeaderComponent, PagerComponent, EmptyStateComponent, StatusMarkComponent],
  template: `
    <ng-template #list>
      <div class="flex flex-wrap items-center gap-2 border-b border-base-300 px-4 py-3">
        @if (!caseTypeId()) {
          <label class="sr-only" for="caseTypeFilter">{{ 'checkpointTypes.column.caseType' | translate }}</label>
          <select id="caseTypeFilter" class="select select-sm w-full sm:w-64" [ngModel]="selectedCaseTypeId" (ngModelChange)="caseTypeChanged($event)">
            <option [ngValue]="undefined">{{ 'checkpointTypes.allCaseTypes' | translate }}</option>
            @for (caseType of caseTypes(); track caseType.id) {
              <option [ngValue]="caseType.id">{{ caseType.title }} ({{ caseType.code }})</option>
            }
          </select>
        }
        @if (!embedded()) {
          <label class="input input-sm w-full sm:w-64">
            <ng-icon name="lucideSearch" size="0.95rem" class="text-quiet" />
            <input type="search" [placeholder]="'common.search' | translate" [attr.aria-label]="'common.search' | translate"
                   [value]="search() ?? ''" (input)="searchChanged($any($event.target).value)" />
          </label>
        }
        @if (embedded()) {
          <a class="btn btn-primary btn-sm ml-auto" [routerLink]="['/checkpoint-types/create']" [queryParams]="createQueryParams()">
            <ng-icon name="lucidePlus" size="1rem" /> {{ 'checkpointTypes.create.action' | translate }}
          </a>
        }
      </div>

      @if (items(); as rows) {
        @if (rows.length === 0) {
          <app-empty-state icon="lucideFlag" [title]="'checkpointTypes.empty.title' | translate" [message]="'checkpointTypes.empty.message' | translate" />
        } @else {
          <div class="overflow-x-auto transition-opacity" [class.opacity-60]="loading()">
            <table class="table data-table">
              <thead>
                <tr>
                  @if (!caseTypeId()) {
                    <th>{{ 'checkpointTypes.column.caseType' | translate }}</th>
                  }
                  <th>{{ 'checkpointTypes.column.code' | translate }}</th>
                  <th>{{ 'checkpointTypes.column.title' | translate }}</th>
                  <th>{{ 'checkpointTypes.column.status' | translate }}</th>
                  <th>{{ 'checkpointTypes.column.private' | translate }}</th>
                  <th class="w-px"><span class="sr-only">{{ 'checkpointTypes.column.actions' | translate }}</span></th>
                </tr>
              </thead>
              <tbody>
                @for (item of rows; track item.id) {
                  <tr class="is-link" tabindex="0" (click)="edit(item)" (keydown.enter)="edit(item)">
                    @if (!caseTypeId()) {
                      <td class="ref text-quiet">{{ item.caseTypeCode }}</td>
                    }
                    <td class="ref">{{ item.code }}</td>
                    <td>{{ item.title }}</td>
                    <td><app-status-mark [status]="item.status" /></td>
                    <td>
                      @if (item.private) {
                        <span class="inline-flex items-center gap-1.5"><ng-icon name="lucideLock" size="0.95rem" class="text-quiet" /> {{ 'common.yes' | translate }}</span>
                      } @else {
                        <span class="text-quiet">{{ 'common.no' | translate }}</span>
                      }
                    </td>
                    <td class="whitespace-nowrap text-right">
                      <div class="tooltip tooltip-left" [attr.data-tip]="'common.edit' | translate">
                        <a class="btn btn-ghost btn-sm btn-square" [routerLink]="['/checkpoint-types', item.id, 'edit']" (click)="$event.stopPropagation()"
                           [attr.aria-label]="('common.edit' | translate) + ' ' + item.code">
                          <ng-icon name="lucidePencil" size="1rem" />
                        </a>
                      </div>
                      <div class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                        <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="delete(item, $event)"
                                [attr.aria-label]="('common.delete' | translate) + ' ' + item.code">
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
    </ng-template>

    @if (embedded()) {
      <ng-container *ngTemplateOutlet="list" />
    } @else {
      <div class="page">
        <app-page-header [title]="'checkpointTypes.title' | translate">
          <a actions routerLink="/checkpoint-types/create" class="btn btn-primary">
            <ng-icon name="lucidePlus" size="1rem" /> {{ 'checkpointTypes.create.action' | translate }}
          </a>
        </app-page-header>
        <section class="panel overflow-hidden">
          <ng-container *ngTemplateOutlet="list" />
        </section>
      </div>
    }
  `
})
export class CheckpointTypesComponent extends ListPage<CheckpointType> implements OnInit {
  /** Scopes the list to one case type (embedded in the case-type edit page). */
  public readonly caseTypeId = input<string | undefined>();
  /** Renders without the page frame. */
  public readonly embedded = input(false);

  private readonly api = inject(CasesApiService);
  private readonly caseTypeService = inject(CaseTypeService);
  private readonly dialogs = inject(DialogService);
  private readonly toaster = inject(ToastService);

  protected readonly caseTypes = signal<CaseTypePartial[]>([]);
  protected selectedCaseTypeId: string | undefined;

  constructor() {
    super();
    this.pageSize.set(20);
    this.sort.set('caseTypeCode');
    this.sortdir.set('asc');
  }

  public ngOnInit(): void {
    this.selectedCaseTypeId = this.caseTypeId() ?? this.route.snapshot.queryParamMap.get('caseTypeId') ?? undefined;
    if (!this.caseTypeId()) {
      this.caseTypeService.getCaseTypes().pipe(take(1)).subscribe((result: { items?: CaseTypePartial[] }) => this.caseTypes.set(result.items ?? []));
    }
    this.startList();
  }

  public loadItems(): Observable<IResultSet<CheckpointType> | null | undefined> {
    const sort = this.sort();
    return this.api.getCheckpointTypesList(
      this.page(),
      this.pageSize(),
      sort ? (this.sortdir() === 'desc' ? `${sort}-` : sort) : undefined,
      this.search() || undefined,
      this.selectedCaseTypeId || undefined
    ).pipe(map(result => result as IResultSet<CheckpointType>));
  }

  protected caseTypeChanged(caseTypeId: string | undefined): void {
    this.selectedCaseTypeId = caseTypeId || undefined;
    this.refresh();
  }

  protected createQueryParams(): { [key: string]: string } {
    return this.selectedCaseTypeId ? { caseTypeId: this.selectedCaseTypeId } : {};
  }

  protected edit(item: CheckpointType): void {
    this.router.navigate(['/checkpoint-types', item.id, 'edit']);
  }

  protected delete(item: CheckpointType, event: Event): void {
    event.stopPropagation();
    const data: ConfirmDialogData = {
      title: 'checkpointTypes.delete.title',
      message: 'checkpointTypes.delete.confirm',
      subject: item.code,
      confirmLabel: 'common.delete',
      cancelLabel: 'common.return',
      tone: 'danger'
    };
    this.dialogs.open<ConfirmDialogComponent, boolean>(ConfirmDialogComponent, { data }).closed
      .pipe(
        filter(confirmed => confirmed === true),
        switchMap(() => this.api.deleteCheckpointType(item.id!).pipe(
          catchError(err => {
            this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
            return EMPTY;
          })
        ))
      )
      .subscribe(() => {
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.checkpointTypeDeleted.body');
        this.refresh();
      });
  }
}
