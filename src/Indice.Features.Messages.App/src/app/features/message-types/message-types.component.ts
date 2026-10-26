import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, map, switchMap } from 'rxjs';
import { LucidePencil, LucidePlus, LucideTags, LucideTrash } from '@lucide/angular';
import { MessagesApiClient, MessageType } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

@Component({
  selector: 'app-message-types',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, MessageTypeBadgeComponent,
    LucidePencil, LucidePlus, LucideTags, LucideTrash
  ],
  template: `
    <app-page-header [title]="'MessageTypes.Title' | translate" [subtitle]="'MessageTypes.Subtitle' | translate">
      <div actions>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('create-message-type')">
          <svg lucidePlus class="size-4"></svg>{{ 'MessageTypes.CreateDialogTitle' | translate }}
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
            <th>{{ 'MessageTypes.NameColumn' | translate }}</th>
            <th>{{ 'MessageTypes.AliasColumn' | translate }}</th>
            <th>{{ 'MessageTypes.CategoryColumn' | translate }}</th>
            <th class="w-24 text-right"><span class="sr-only">{{ 'MessageTypes.ActionsColumn' | translate }}</span></th>
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
                  <a class="link link-hover font-semibold" [routerLink]="pane.link(['edit-message-type', item.id!])">{{ item.name }}</a>
                </td>
                <td>
                  @if (item.alias) {
                    <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ item.alias }}</code>
                  } @else {
                    <span class="text-base-content/40">—</span>
                  }
                </td>
                <td><app-message-type-badge [name]="item.classification" [classification]="item.classification" /></td>
                <td class="text-right whitespace-nowrap">
                  <span class="tooltip" [attr.data-tip]="'MessageTypes.EditTypeTitle' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['edit-message-type', item.id!])"
                      [attr.aria-label]="'MessageTypes.EditTypeTitle' | translate">
                      <svg lucidePencil class="size-4"></svg>
                    </a>
                  </span>
                  <span class="tooltip tooltip-left" [attr.data-tip]="'MessageTypes.DeleteTypeTitle' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="remove(item)"
                      [attr.aria-label]="'MessageTypes.DeleteTypeTitle' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Shared.ListViewEmptyStateSubTitle' | translate">
                    <svg icon lucideTags class="size-5"></svg>
                    <a actions class="btn btn-sm" [routerLink]="pane.link('create-message-type')">
                      <svg lucidePlus class="size-4"></svg>{{ 'MessageTypes.CreateDialogTitle' | translate }}
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
export class MessageTypesComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);

  protected readonly list = new ListState<MessageType>({
    pageSize: 20,
    sort: 'name',
    dir: 'asc',
    load: query => this._api.getMessageTypes(query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined)
  });

  constructor() {
    inject(RefreshBus).on('message-types').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }

  protected remove(type: MessageType): void {
    this._dialog
      .confirm({ title: 'MessageTypes.DeleteTypeTitle', message: 'MessageTypes.DeleteConfirmMessage', messageParams: { name: type.name }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.deleteMessageType(type.id!)),
        map(() => type)
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'MessageTypes.DeleteSuccessTitle', 'MessageTypes.DeleteSuccessMessage', undefined, { name: type.name });
        this.list.refresh();
      });
  }
}
