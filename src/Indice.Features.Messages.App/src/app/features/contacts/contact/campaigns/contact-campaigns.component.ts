import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowDown, LucideArrowUp, LucideArrowUpDown, LucideMegaphone } from '@lucide/angular';
import { Campaign, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BoolIconComponent, CHANNEL_KINDS, ChannelIconComponent, MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { ContactStore } from '../contact-store.service';
import { ListCountComponent } from 'src/app/ui/list/list-count.component';

/** "Campaigns" tab of the contact detail page: the campaigns the contact received. */
@Component({
  selector: 'app-contact-campaigns',
  imports: [
    ListCountComponent, DatePipe, RouterLink, TranslatePipe, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    BoolIconComponent, ChannelIconComponent, MessageTypeBadgeComponent, LucideArrowDown, LucideArrowUp, LucideArrowUpDown, LucideMegaphone
  ],
  template: `
    <app-list-count class="mb-3" [count]="list.items() ? list.count() : null" one="Campaigns.CountOne" other="Campaigns.CountOther" />
    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel">
      <div class="overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              @for (column of columns; track column.label) {
                <th [class]="column.class ?? ''">
                  @if (column.sort) {
                    <button type="button" class="inline-flex items-center gap-1 hover:text-base-content" (click)="list.setSort(column.sort)">
                      {{ column.label | translate }}
                      @if (list.sort() === column.sort) {
                        @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
                      } @else {
                        <svg lucideArrowUpDown class="size-3.5 opacity-40"></svg>
                      }
                    </button>
                  } @else {
                    {{ column.label | translate }}
                  }
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @if (list.loading() && !list.items()?.length) {
              @for (row of [1, 2, 3, 4]; track row) {
                <tr><td colspan="8"><div class="skeleton h-4 w-full"></div></td></tr>
              }
            } @else {
              @for (item of list.items(); track item.id) {
                <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                  <td>
                    <a class="link link-hover font-semibold" [routerLink]="['/campaigns', item.id, 'campaign-details']"
                      [attr.title]="'Contacts.CampaignDetails' | translate">{{ item.title }}</a>
                  </td>
                  <td>
                    <div class="flex gap-1">
                      @for (kind of channelKinds; track kind) {
                        <app-channel-icon [kind]="kind" [muted]="!uses(item, kind)" />
                      }
                    </div>
                  </td>
                  <td><app-message-type-badge [name]="item.type?.name" [classification]="item.type?.classification" /></td>
                  <td class="whitespace-nowrap">{{ item.activePeriod?.from ? (item.activePeriod?.from | date: 'medium') : '—' }}</td>
                  <td class="whitespace-nowrap">{{ item.activePeriod?.to ? (item.activePeriod?.to | date: 'medium') : '—' }}</td>
                  <td>{{ item.createdBy || '—' }}</td>
                  <td class="whitespace-nowrap text-base-content/70">{{ item.createdAt | date: 'medium' }}</td>
                  <td class="text-center"><app-bool-icon [value]="item.published" /></td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="8">
                    <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Contacts.NoCampaignsMessage' | translate">
                      <svg icon lucideMegaphone class="size-5"></svg>
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
export class ContactCampaignsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _store = inject(ContactStore);
  protected readonly channelKinds = CHANNEL_KINDS;

  protected readonly columns: { label: string; sort?: string; class?: string }[] = [
    { label: 'Contacts.CampaignTitle', sort: 'title' },
    { label: 'Contacts.Channels' },
    { label: 'Contacts.Type', sort: 'type.name' },
    { label: 'Contacts.From', sort: 'activePeriod.from' },
    { label: 'Contacts.To', sort: 'activePeriod.to' },
    { label: 'Contacts.CreatedBy', sort: 'createdBy' },
    { label: 'Contacts.CreatedAt', sort: 'createdAt' },
    { label: 'Contacts.Published', sort: 'published', class: 'text-center' }
  ];

  protected readonly list = new ListState<Campaign>({
    pageSize: 20,
    sort: 'createdAt',
    dir: 'asc',
    load: query => this._api.getCampaigns(query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined,
      undefined, undefined, this._store.contactId())
  });

  public ngOnInit(): void {
    this.list.start();
  }

  protected uses(campaign: Campaign, kind: string): boolean {
    return !!campaign.messageChannelKind?.some(channel => channel === kind);
  }
}
