import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable } from 'rxjs';
import { catchError, filter, map, switchMap, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial, CaseTypePartialResultSet } from 'src/app/core/services/cases-api.service';
import { ConfirmDialogComponent, ConfirmDialogData } from 'src/app/ui/feedback/confirm-dialog.component';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { IResultSet } from 'src/app/ui/list/filtering';
import { ListPage } from 'src/app/ui/list/list-page';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';

@Component({
  selector: 'app-case-types',
  imports: [RouterLink, NgIcon, TranslatePipe, PageHeaderComponent, EmptyStateComponent],
  template: `
    <div class="page">
      <app-page-header [title]="'caseTypes.title' | translate">
        <a actions routerLink="/case-types/create" class="btn btn-primary">
          <ng-icon name="lucidePlus" size="1rem" /> {{ 'caseTypes.create.action' | translate }}
        </a>
      </app-page-header>

      <section class="panel overflow-hidden">
        @if (items(); as rows) {
          @if (rows.length === 0) {
            <app-empty-state icon="lucideLayers" [title]="'caseTypes.empty.title' | translate" [message]="'caseTypes.empty.message' | translate">
              <a routerLink="/case-types/create" class="btn btn-primary btn-sm"><ng-icon name="lucidePlus" size="1rem" /> {{ 'caseTypes.create.action' | translate }}</a>
            </app-empty-state>
          } @else {
            <div class="overflow-x-auto transition-opacity" [class.opacity-60]="loading()">
              <table class="table data-table">
                <thead>
                  <tr>
                    <th>{{ 'caseTypes.form.titleLabel' | translate }}</th>
                    <th>{{ 'caseTypes.form.code' | translate }}</th>
                    <th>{{ 'caseTypes.form.tags' | translate }}</th>
                    <th>{{ 'caseTypes.column.menu' | translate }}</th>
                    <th class="w-px"><span class="sr-only">{{ 'checkpointTypes.column.actions' | translate }}</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (item of rows; track item.id) {
                    <tr class="is-link" tabindex="0" (click)="open(item)" (keydown.enter)="open(item)">
                      <td class="font-medium">{{ item.title }}</td>
                      <td class="ref text-quiet">{{ item.code }}</td>
                      <td>
                        @if (item.tags) {
                          <div class="flex flex-wrap gap-1">
                            @for (tag of item.tags.split(','); track $index) {
                              <span class="badge badge-soft badge-sm">{{ tag }}</span>
                            }
                          </div>
                        }
                      </td>
                      <td>
                        @if (item.isMenuItem) {
                          <span class="inline-flex items-center gap-1.5"><ng-icon name="lucideCheck" size="1rem" class="text-primary" /> {{ 'common.yes' | translate }}</span>
                        } @else {
                          <span class="text-quiet">{{ 'common.no' | translate }}</span>
                        }
                      </td>
                      <td class="whitespace-nowrap text-right">
                        <div class="tooltip tooltip-left" [attr.data-tip]="'common.edit' | translate">
                          <a class="btn btn-ghost btn-sm btn-square" [routerLink]="['/case-types', item.id, 'edit']" (click)="$event.stopPropagation()"
                             [attr.aria-label]="('common.edit' | translate) + ' ' + item.title">
                            <ng-icon name="lucidePencil" size="1rem" />
                          </a>
                        </div>
                        <div class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                          <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="delete(item, $event)"
                                  [attr.aria-label]="('common.delete' | translate) + ' ' + item.title">
                            <ng-icon name="lucideTrash2" size="1rem" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
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
export class CaseTypesComponent extends ListPage<CaseTypePartial> implements OnInit {
  private readonly api = inject(CasesApiService);
  private readonly caseTypeService = inject(CaseTypeService);
  private readonly dialogs = inject(DialogService);
  private readonly toaster = inject(ToastService);

  public ngOnInit(): void {
    this.startList();
  }

  public loadItems(): Observable<IResultSet<CaseTypePartial> | null | undefined> {
    return this.caseTypeService.getCaseTypes().pipe(
      take(1),
      map((result: CaseTypePartialResultSet) => result as IResultSet<CaseTypePartial>)
    );
  }

  protected open(item: CaseTypePartial): void {
    this.router.navigate(['/case-types', item.id, 'edit']);
  }

  protected delete(item: CaseTypePartial, event: Event): void {
    event.stopPropagation();
    const data: ConfirmDialogData = {
      title: 'caseTypes.delete.title',
      message: 'caseTypes.delete.confirm',
      subject: item.title,
      confirmLabel: 'common.delete',
      cancelLabel: 'common.return',
      tone: 'danger'
    };
    this.dialogs.open<ConfirmDialogComponent, boolean>(ConfirmDialogComponent, { data }).closed
      .pipe(
        filter(confirmed => confirmed === true),
        switchMap(() => this.api.deleteCaseType(item.id!).pipe(
          catchError(err => {
            this.toaster.show(ToastType.Error, 'toasts.error.title', err?.detail);
            return EMPTY;
          })
        ))
      )
      .subscribe(() => {
        this.caseTypeService.invalidateCaseTypes();
        this.toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeDeleted.body');
        this.refresh();
      });
  }
}
