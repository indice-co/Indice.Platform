import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucideListPlus, LucideTrash, LucideUsers } from '@lucide/angular';
import { DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

@Component({
  selector: 'app-distribution-lists',
  imports: [
    DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    LucideListPlus, LucideTrash, LucideUsers
  ],
  template: `
    <app-page-header [title]="'DistributionLists.ListsTitle' | translate" [subtitle]="'DistributionLists.Subtitle' | translate">
      <div actions>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('create-distribution-list')">
          <svg lucideListPlus class="size-4"></svg>{{ 'DistributionLists.CreateTitle' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ 'DistributionLists.Name' | translate }}</th>
            <th>{{ 'DistributionLists.Alias' | translate }}</th>
            <th>{{ 'DistributionLists.CreatedBy' | translate }}</th>
            <th>{{ 'DistributionLists.CreatedAt' | translate }}</th>
            <th>{{ 'DistributionLists.UpdatedBy' | translate }}</th>
            <th>{{ 'DistributionLists.UpdatedAt' | translate }}</th>
            <th class="w-16 text-right"><span class="sr-only">{{ 'DistributionLists.Actions' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="7"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  <a class="link link-hover font-semibold" [routerLink]="[item.id]" [attr.title]="'DistributionLists.Details' | translate">{{ item.name }}</a>
                </td>
                <td>
                  @if (item.alias) {
                    <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ item.alias }}</code>
                  } @else {
                    <span class="text-base-content/40">—</span>
                  }
                </td>
                <td>{{ item.createdBy || '—' }}</td>
                <td class="whitespace-nowrap text-base-content/70">{{ item.createdAt | date: 'medium' }}</td>
                <td>{{ item.updatedBy || '—' }}</td>
                <td class="whitespace-nowrap text-base-content/70">{{ item.updatedAt ? (item.updatedAt | date: 'medium') : '—' }}</td>
                <td class="text-right">
                  <span class="tooltip tooltip-left" [attr.data-tip]="'DistributionLists.Delete' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="delete(item)"
                      [attr.aria-label]="'DistributionLists.Delete' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Shared.ListViewEmptyStateSubTitle' | translate">
                    <svg icon lucideUsers class="size-5"></svg>
                    <a actions class="btn btn-sm" [routerLink]="pane.link('create-distribution-list')">
                      <svg lucideListPlus class="size-4"></svg>{{ 'DistributionLists.CreateTitle' | translate }}
                    </a>
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
export class DistributionListsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);

  protected readonly list = new ListState<DistributionList>({
    pageSize: 20,
    sort: 'name',
    dir: 'asc',
    // system generated lists are not managed here
    load: query => this._api.getDistributionLists(query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined, false)
  });

  constructor() {
    inject(RefreshBus).on('distribution-lists').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }

  protected delete(list: DistributionList): void {
    this._dialog
      .confirm({ title: 'DistributionLists.Delete', message: 'DistributionLists.DeleteConfirmMessage', messageParams: { name: list.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteDistributionList(list.id!))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'DistributionLists.DeleteSuccessTitle', 'DistributionLists.DeleteSuccessMessage', undefined, { name: list.name });
        this.list.refresh();
      });
  }
}
