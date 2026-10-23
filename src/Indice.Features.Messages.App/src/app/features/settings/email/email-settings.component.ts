import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap, tap } from 'rxjs';
import { LucideArrowDown, LucideArrowUp, LucideBadgeCheck, LucideMail, LucidePencil, LucidePlus, LucideTrash } from '@lucide/angular';
import { MessageSender, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { SettingsStore } from '../settings-store.service';

/** Email senders section of the settings page. */
@Component({
  selector: 'app-email-settings',
  imports: [
    DatePipe, RouterLink, TranslatePipe, EmptyStateComponent, PaginationComponent,
    LucideArrowDown, LucideArrowUp, LucideBadgeCheck, LucideMail, LucidePencil, LucidePlus, LucideTrash
  ],
  template: `
    <section class="panel">
      <div class="flex flex-col gap-3 border-b border-base-300 p-5 sm:flex-row sm:items-start sm:justify-between sm:p-6">
        <div>
          <h2 class="font-semibold">{{ 'Settings.EmailSendersHeader' | translate }}</h2>
          <p class="text-sm text-base-content/70">{{ 'Settings.EmailSendersDescription' | translate }}</p>
        </div>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('create-message-sender')">
          <svg lucidePlus class="size-4"></svg>{{ 'Settings.Add' | translate }}
        </a>
      </div>

      @if (defaultSender(); as sender) {
        <div class="flex items-center gap-2 border-b border-base-300 bg-base-200/50 px-5 py-3 text-sm sm:px-6">
          <svg lucideBadgeCheck class="size-4 shrink-0 text-success"></svg>
          <p class="text-base-content/70">
            {{ 'Settings.DefaultSenderInfoPrefix' | translate }}
            <strong class="text-base-content">{{ sender.displayName }} &lt;{{ sender.sender }}&gt;</strong>
            {{ 'Settings.DefaultSenderInfoSuffix' | translate }}
          </p>
        </div>
      }

      <div class="overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              @for (column of sortableColumns; track column.sort) {
                <th>
                  <button type="button" class="inline-flex items-center gap-1 uppercase" (click)="list.setSort(column.sort)">
                    {{ column.label | translate }}
                    @if (list.sort() === column.sort) {
                      @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
                    }
                  </button>
                </th>
              }
              <th>{{ 'Settings.CreatedByColumn' | translate }}</th>
              <th class="w-24 text-right"><span class="sr-only">{{ 'Settings.ActionsColumn' | translate }}</span></th>
            </tr>
          </thead>
          <tbody>
            @if (list.loading() && !list.items()?.length) {
              @for (row of [1, 2, 3]; track row) {
                <tr><td colspan="5"><div class="skeleton h-4 w-full"></div></td></tr>
              }
            } @else {
              @for (item of list.items(); track item.id) {
                <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                  <td>
                    <div class="flex items-center gap-2">
                      <a class="link link-hover font-semibold" [routerLink]="pane.link(['edit-message-sender', item.id!])">{{ item.sender || '—' }}</a>
                      @if (item.isDefault) {
                        <span class="badge badge-success badge-soft badge-sm gap-1" [title]="'Settings.DefaultSenderTooltip' | translate">
                          <svg lucideBadgeCheck class="size-3.5"></svg>{{ 'Settings.DefaultBadge' | translate }}
                        </span>
                      }
                    </div>
                  </td>
                  <td>{{ item.displayName || '—' }}</td>
                  <td class="whitespace-nowrap">{{ item.createdAt ? (item.createdAt | date: 'medium') : '—' }}</td>
                  <td>{{ item.createdBy || '—' }}</td>
                  <td class="text-right whitespace-nowrap">
                    <span class="tooltip" [attr.data-tip]="'Settings.EditSenderTitle' | translate">
                      <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['edit-message-sender', item.id!])"
                        [attr.aria-label]="'Settings.EditSenderTitle' | translate">
                        <svg lucidePencil class="size-4"></svg>
                      </a>
                    </span>
                    @if (!item.isDefault) {
                      <span class="tooltip tooltip-left" [attr.data-tip]="'Settings.DeleteSenderTitle' | translate">
                        <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="delete(item)"
                          [attr.aria-label]="'Settings.DeleteSenderTitle' | translate">
                          <svg lucideTrash class="size-4"></svg>
                        </button>
                      </span>
                    }
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="5">
                    <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Shared.ListViewEmptyStateSubTitle' | translate">
                      <svg icon lucideMail class="size-5"></svg>
                      <a actions class="btn btn-sm" [routerLink]="pane.link('create-message-sender')">
                        <svg lucidePlus class="size-4"></svg>{{ 'Settings.Add' | translate }}
                      </a>
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
    </section>
  `
})
export class EmailSettingsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _store = inject(SettingsStore);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  protected readonly pane = inject(SidePaneService);

  protected readonly sortableColumns = [
    { sort: 'sender', label: 'Settings.SenderColumn' },
    { sort: 'displayName', label: 'Settings.NameColumn' },
    { sort: 'createdAt', label: 'Settings.CreatedAtColumn' }
  ];
  protected readonly defaultSender = signal<MessageSender | undefined>(undefined);
  protected readonly list = new ListState<MessageSender>({
    pageSize: 20,
    sort: 'isDefault',
    dir: 'desc',
    load: query => this._api
      .getMessageSenders(query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined)
      .pipe(tap(result => this.defaultSender.set(result?.items?.find(i => i.isDefault))))
  });

  constructor() {
    inject(RefreshBus).on('message-senders').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }

  protected delete(sender: MessageSender): void {
    this._dialog
      .confirm({ title: 'Settings.DeleteSenderTitle', message: 'Settings.DeleteSenderConfirmMessage', messageParams: { sender: sender.displayName }, confirmLabel: 'Common.Delete', tone: 'danger' })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._store.deleteMessageSender(sender.id!))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'Settings.DeleteSenderSuccessTitle', 'Settings.DeleteSenderSuccessMessage', undefined, { sender: sender.displayName });
        this.list.refresh();
      });
  }
}
