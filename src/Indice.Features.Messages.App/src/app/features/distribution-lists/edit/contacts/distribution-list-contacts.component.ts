import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucideDownload, LucideTrash, LucideUpload, LucideUserPlus, LucideUsers } from '@lucide/angular';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactAvatarComponent, contactDisplayName } from 'src/app/shared/components/list-contact-create/contact-avatar.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { BoolIconComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ListSortComponent, SortOption } from 'src/app/features/contacts/list-sort.component';
import { DistributionListEditStore } from '../distribution-list-edit-store.service';

/** "Contacts" tab of the distribution list page. */
@Component({
  selector: 'app-distribution-list-contacts',
  imports: [
    DatePipe, RouterLink, TranslatePipe, EmptyStateComponent, PaginationComponent, SearchInputComponent, ListSortComponent, ContactAvatarComponent,
    BoolIconComponent, LucideDownload, LucideTrash, LucideUpload, LucideUserPlus, LucideUsers
  ],
  template: `
    <div class="mb-3 flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
      <div class="flex flex-col gap-2 sm:flex-row sm:items-center">
        <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
        <app-list-sort [list]="list" [options]="sortOptions" />
      </div>
      <div class="flex flex-wrap gap-2">
        <button type="button" class="btn btn-sm btn-ghost" [disabled]="exporting()" (click)="export()" [attr.title]="'DistributionLists.ExportContactsTooltip' | translate">
          @if (exporting()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideDownload class="size-4"></svg> }
          {{ 'DistributionLists.Export' | translate }}
        </button>
        <a class="btn btn-sm" [routerLink]="pane.link('import-contacts', paneParams())" [attr.title]="'DistributionLists.ImportContactsTooltip' | translate">
          <svg lucideUpload class="size-4"></svg>{{ 'DistributionLists.ImportContacts' | translate }}
        </a>
        <a class="btn btn-sm btn-primary" [routerLink]="pane.link('create-contact', paneParams())">
          <svg lucideUserPlus class="size-4"></svg>{{ 'DistributionLists.AddContacts' | translate }}
        </a>
      </div>
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ 'DistributionLists.Name' | translate }}</th>
            <th>{{ 'DistributionLists.Email' | translate }}</th>
            <th>{{ 'DistributionLists.Phone' | translate }}</th>
            <th>{{ 'DistributionLists.ContactCode' | translate }}</th>
            <th>{{ 'DistributionLists.UpdatedAt' | translate }}</th>
            <th class="w-16 text-right"><span class="sr-only">{{ 'DistributionLists.Actions' | translate }}</span></th>
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
                <td>
                  <div class="flex items-center gap-3">
                    <app-contact-avatar [name]="displayName(item)" size="sm" />
                    <a class="link link-hover truncate font-semibold" [routerLink]="pane.link(['edit-contact', item.id!], paneParams())">{{ displayName(item) || '—' }}</a>
                  </div>
                </td>
                <td class="max-w-56 truncate">{{ item.email || '—' }}</td>
                <td class="whitespace-nowrap tabular">{{ item.phoneNumber || '—' }}</td>
                <td>
                  @if (item.recipientId) {
                    <span class="inline-flex items-center gap-1.5" [attr.title]="(item.resolved ? 'DistributionLists.Resolved' : 'DistributionLists.Unknown') | translate">
                      <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ item.recipientId }}</code>
                      <app-bool-icon [value]="item.resolved" />
                    </span>
                  } @else {
                    <span class="text-base-content/40">—</span>
                  }
                </td>
                <td class="whitespace-nowrap text-base-content/70">{{ item.updatedAt ? (item.updatedAt | date: 'medium') : '—' }}</td>
                <td class="text-right">
                  <span class="tooltip tooltip-left" [attr.data-tip]="'DistributionLists.RemoveContact' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="remove(item)"
                      [attr.aria-label]="'DistributionLists.RemoveContact' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="'DistributionLists.NoContactsTitle' | translate" [message]="'DistributionLists.NoContactsMessage' | translate">
                    <svg icon lucideUsers class="size-5"></svg>
                    <div actions class="flex gap-2">
                      <a class="btn btn-sm" [routerLink]="pane.link('import-contacts', paneParams())">
                        <svg lucideUpload class="size-4"></svg>{{ 'DistributionLists.ImportContacts' | translate }}
                      </a>
                      <a class="btn btn-sm btn-primary" [routerLink]="pane.link('create-contact', paneParams())">
                        <svg lucideUserPlus class="size-4"></svg>{{ 'DistributionLists.AddContacts' | translate }}
                      </a>
                    </div>
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
export class DistributionListContactsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _store = inject(DistributionListEditStore);
  protected readonly pane = inject(SidePaneService);
  protected readonly displayName = contactDisplayName;
  protected readonly exporting = signal(false);

  protected readonly sortOptions: SortOption[] = [
    { value: 'firstName', label: 'DistributionLists.SortFirstNameOption' },
    { value: 'lastName', label: 'DistributionLists.SortLastNameOption' },
    { value: 'email', label: 'DistributionLists.SortEmailOption' },
    { value: 'phone', label: 'DistributionLists.SortPhoneOption' },
    { value: 'updatedAt', label: 'DistributionLists.SortUpdatedAtOption' },
    { value: 'recipientId', label: 'DistributionLists.SortContactCodeOption' },
    { value: 'resolved', label: 'DistributionLists.SortResolvedOption' },
    { value: 'lastResolutionDate', label: 'DistributionLists.SortLastResolutionDateOption' }
  ];

  protected readonly list = new ListState<Contact>({
    pageSize: 20,
    sort: 'updatedAt',
    dir: 'desc',
    load: query => this._api.getDistributionListContacts(this._store.distributionListId(), query.page, query.pageSize,
      query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined)
  });

  constructor() {
    inject(RefreshBus).on('distribution-list-contacts').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }

  /** Matrix params that tell the side panes which list they work on. */
  protected paneParams(): Record<string, string> {
    return { distributionListId: this._store.distributionListId() };
  }

  protected remove(contact: Contact): void {
    const name = contact.fullName || contact.email;
    this._dialog
      .confirm({
        title: 'DistributionLists.DeleteContactTitle',
        message: 'DistributionLists.DeleteContactConfirmMessage',
        messageParams: { name, list: this._store.distributionList()?.name },
        confirmLabel: 'DistributionLists.RemoveContact',
        tone: 'danger'
      })
      .pipe(
        filter(confirmed => confirmed),
        switchMap(() => this._api.removeContactFromDistributionList(this._store.distributionListId(), contact.id!))
      )
      .subscribe(() => {
        this._toaster.show(ToastType.Success, 'DistributionLists.DeleteContactSuccessTitle', 'DistributionLists.DeleteContactSuccessMessage', undefined, { name });
        this.list.refresh();
      });
  }

  protected export(): void {
    this.exporting.set(true);
    this._api.bulkExportContactsFromDistributionList(this._store.distributionListId()).subscribe({
      next: response => {
        const url = URL.createObjectURL(response.data);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = response.fileName ?? 'contacts.csv';
        anchor.click();
        URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: () => this.exporting.set(false)
    });
  }
}
