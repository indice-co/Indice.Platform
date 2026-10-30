import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideContactRound, LucideUserPlus } from '@lucide/angular';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactAvatarComponent, contactDisplayName } from 'src/app/shared/components/list-contact-create/contact-avatar.component';
import { BoolIconComponent, ChannelIconComponent } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { contactChannels } from './contact-channels';
import { ListSortComponent, SortOption } from './list-sort.component';
import { ListCountComponent } from 'src/app/ui/list/list-count.component';

@Component({
  selector: 'app-contacts',
  imports: [
    ListCountComponent, DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, ListSortComponent,
    ContactAvatarComponent, ChannelIconComponent, BoolIconComponent, LucideContactRound, LucideUserPlus
  ],
  template: `
    <app-page-header [title]="'Contacts.ListTitle' | translate" [subtitle]="'Contacts.Subtitle' | translate">
      <app-list-count class="mt-0.5" [count]="list.items() ? list.count() : null" one="Contacts.CountOne" other="Contacts.CountOther" />
      <div actions>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('create-new-contact')">
          <svg lucideUserPlus class="size-4"></svg>{{ 'Contacts.AddContacts' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
      <app-list-sort [list]="list" [options]="sortOptions" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ 'Contacts.Name' | translate }}</th>
            <th>{{ 'Contacts.Email' | translate }}</th>
            <th>{{ 'Contacts.Phone' | translate }}</th>
            <th>{{ 'Contacts.Channels' | translate }}</th>
            <th>{{ 'Contacts.ContactCode' | translate }}</th>
            <th class="text-right">{{ 'Contacts.UpdatedAt' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4, 5]; track row) {
              <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  <div class="flex items-center gap-3">
                    <app-contact-avatar [name]="displayName(item)" size="sm" />
                    <div class="min-w-0">
                      <a class="link link-hover block truncate font-semibold" [routerLink]="[item.id]">{{ displayName(item) || '—' }}</a>
                      @if (item.isAnonymous) {
                        <span class="badge badge-xs badge-ghost">{{ 'Contacts.Anonymous' | translate }}</span>
                      } @else {
                        <span class="badge badge-xs badge-soft badge-primary">{{ 'Contacts.Known' | translate }}</span>
                      }
                    </div>
                  </div>
                </td>
                <td class="max-w-56 truncate">{{ item.email || '—' }}</td>
                <td class="whitespace-nowrap tabular">{{ item.phoneNumber || '—' }}</td>
                <td>
                  <div class="flex gap-1">
                    @for (channel of channels(item); track channel.kind) {
                      <app-channel-icon [kind]="channel.kind" [muted]="!channel.available" />
                    }
                  </div>
                </td>
                <td>
                  @if (item.recipientId) {
                    <span class="inline-flex items-center gap-1.5" [attr.title]="(item.resolved ? 'Contacts.Resolved' : 'Contacts.Unknown') | translate">
                      <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ item.recipientId }}</code>
                      <app-bool-icon [value]="item.resolved" />
                    </span>
                  } @else {
                    <span class="text-base-content/40">—</span>
                  }
                </td>
                <td class="text-right whitespace-nowrap text-base-content/70">{{ item.updatedAt ? (item.updatedAt | date: 'medium') : '—' }}</td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Shared.ListViewEmptyStateSubTitle' | translate">
                    <svg icon lucideContactRound class="size-5"></svg>
                    <a actions class="btn btn-sm" [routerLink]="pane.link('create-new-contact')">
                      <svg lucideUserPlus class="size-4"></svg>{{ 'Contacts.AddContacts' | translate }}
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
export class ContactsListComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  protected readonly pane = inject(SidePaneService);
  protected readonly displayName = contactDisplayName;
  protected readonly channels = contactChannels;

  protected readonly sortOptions: SortOption[] = [
    { value: 'updatedAt', label: 'Contacts.SortUpdatedAtOption' },
    { value: 'fullName', label: 'Contacts.SortFullNameOption' },
    { value: 'email', label: 'Contacts.SortEmailOption' },
    { value: 'phone', label: 'Contacts.SortPhoneOption' },
    { value: 'recipientId', label: 'Contacts.SortContactCodeOption' },
    { value: 'resolved', label: 'Contacts.SortResolvedOption' },
    { value: 'lastResolutionDate', label: 'Contacts.SortLastResolutionDateOption' }
  ];

  protected readonly list = new ListState<Contact>({
    pageSize: 20,
    sort: 'updatedAt',
    dir: 'desc',
    load: query => this._api.getContacts(query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined)
  });

  constructor() {
    inject(RefreshBus).on('contacts').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }
}
