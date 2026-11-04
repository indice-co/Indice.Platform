import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, filter, map, switchMap } from 'rxjs';
import { LucideFolderTree, LucidePencil, LucidePlus, LucideTrash } from '@lucide/angular';
import { CasesApiService, Category } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

@Component({
  selector: 'app-categories',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    LucideFolderTree, LucidePencil, LucidePlus, LucideTrash
  ],
  template: `
    <app-page-header [title]="'categories.title' | translate" [subtitle]="'categories.subtitle' | translate">
      <div actions>
        <a routerLink="/categories/create" class="btn btn-primary btn-sm">
          <svg lucidePlus class="size-4"></svg>{{ 'categories.create.title' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'common.search' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel">
      <div class="overflow-x-auto">
        <table class="table table-sm">
          <thead>
            <tr>
              <th class="w-20">{{ 'categories.column.order' | translate }}</th>
              <th>{{ 'categories.column.name' | translate }}</th>
              <th class="w-24 text-right"><span class="sr-only">{{ 'categories.column.actions' | translate }}</span></th>
            </tr>
          </thead>
          <tbody>
            @if (list.loading() && !list.items()?.length) {
              @for (row of [1, 2, 3, 4]; track row) {
                <tr><td colspan="3"><div class="skeleton h-4 w-full"></div></td></tr>
              }
            } @else {
              @for (item of list.items(); track item.id) {
                <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                  <td><span class="font-mono text-xs">{{ item.order }}</span></td>
                  <td>
                    <span class="font-medium">{{ item.name }}</span>
                    @if (item.description) {
                      <p class="max-w-md truncate text-xs text-base-content/60">{{ item.description }}</p>
                    }
                  </td>
                  <td class="text-right whitespace-nowrap">
                    <span class="tooltip" [attr.data-tip]="'common.edit' | translate">
                      <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/categories', item.id, 'edit']" [attr.aria-label]="'common.edit' | translate">
                        <svg lucidePencil class="size-4"></svg>
                      </a>
                    </span>
                    <span class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                      <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="onDelete(item)" [attr.aria-label]="'common.delete' | translate">
                        <svg lucideTrash class="size-4"></svg>
                      </button>
                    </span>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="3">
                    <app-empty-state [title]="'categories.empty.title' | translate" [message]="'categories.empty.message' | translate">
                      <svg icon lucideFolderTree class="size-5"></svg>
                    </app-empty-state>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>
      @if (list.count() > 0) {
        <app-pagination class="border-t border-base-300 px-4 py-3" [page]="list.page()" [pageSize]="list.pageSize()" [count]="list.count()"
          (pageChange)="list.setPage($event)" (pageSizeChange)="list.setPageSize($event)" />
      }
    </div>
  `
})
export class CategoriesComponent implements OnInit {
  private readonly _api = inject(CasesApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(TranslatedToasterService);

  protected readonly list = new ListState<Category>({
    pageSize: 20,
    sort: 'order',
    dir: 'asc',
    load: query => this._api.getCategoriesList(
      query.page,
      query.pageSize,
      query.sort ? (query.dir === 'desc' ? `${query.sort}-` : query.sort) : undefined,
      query.search || undefined
    ).pipe(map(result => ({ count: result.count, items: result.items })))
  });

  public ngOnInit(): void {
    this.list.start();
  }

  protected onDelete(item: Category): void {
    this._dialog.confirm({
      title: 'categories.delete.title',
      message: 'categories.delete.confirmName',
      messageParams: { name: item.name },
      confirmLabel: 'common.delete',
      tone: 'danger'
    }).pipe(
      filter(Boolean),
      switchMap(() => this._api.deleteCategory(item.id!)),
      catchError(() => EMPTY)
    ).subscribe(() => {
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.categoryDeleted.body');
      this.list.refresh();
    });
  }
}
