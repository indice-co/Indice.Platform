import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowDown, LucideArrowUp, LucideChevronRight, LucideUsers } from '@lucide/angular';
import { MessagesApiClient, Recipient } from 'src/app/core/services/messages-api.service';
import { CHANNEL_KINDS, ChannelIconComponent } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';

/** Messages tab: the campaign's recipients and the channels each one was messaged on. */
@Component({
  selector: 'app-campaign-messages',
  imports: [DatePipe, RouterLink, TranslatePipe, EmptyStateComponent, PaginationComponent, SearchInputComponent, ChannelIconComponent,
    LucideArrowDown, LucideArrowUp, LucideChevronRight, LucideUsers],
  template: `
    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h2 class="text-lg font-semibold">{{ 'Campaigns.RecipientsTitle' | translate }}</h2>
      </div>
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ 'Campaigns.ContactInfoHeader' | translate }}</th>
            <th>{{ 'Campaigns.ChannelsColumn' | translate }}</th>
            <th>{{ 'Campaigns.PhoneColumn' | translate }}</th>
            <th>{{ 'Campaigns.RecipientIdColumn' | translate }}</th>
            <th>
              <button type="button" class="inline-flex items-center gap-1 hover:text-base-content" (click)="list.setSort('createdOn')">
                {{ 'Campaigns.DateColumn' | translate }}
                @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
              </button>
            </th>
            <th class="w-10"><span class="sr-only">{{ 'Campaigns.DeliveryDetailsTitle' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track $index) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td class="min-w-56">
                  <a class="link link-hover font-semibold" [routerLink]="['..', 'campaign-message-timeline', item.contact?.id]"
                    [title]="'Campaigns.DeliveryDetailsTitle' | translate">
                    {{ item.contact?.fullName || item.contact?.email || item.contact?.id }}
                  </a>
                  @if (item.contact?.fullName && item.contact?.email) {
                    <p class="text-xs text-base-content/60">{{ item.contact?.email }}</p>
                  }
                </td>
                <td>
                  <div class="flex items-center gap-1">
                    @for (kind of channelKinds; track kind) {
                      <app-channel-icon [kind]="kind" [muted]="!item.channels?.includes(kind)" />
                    }
                  </div>
                </td>
                <td class="whitespace-nowrap tabular">{{ item.contact?.phoneNumber || '—' }}</td>
                <td><code class="text-xs">{{ item.contact?.recipientId || '—' }}</code></td>
                <td class="whitespace-nowrap text-sm text-base-content/70 tabular">
                  {{ item.createdOn ? (item.createdOn | date: 'medium') : ('Campaigns.EmptyValueIndicator' | translate) }}
                </td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['..', 'campaign-message-timeline', item.contact?.id]"
                    [attr.aria-label]="'Campaigns.DeliveryDetailsTitle' | translate"><svg lucideChevronRight class="size-4"></svg></a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Campaigns.NoMessagesMessage' | translate">
                    <svg icon lucideUsers class="size-5"></svg>
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
export class CampaignMessagesComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _campaignId = inject(ActivatedRoute).parent?.snapshot.paramMap.get('campaignId') ?? '';

  protected readonly channelKinds = CHANNEL_KINDS;
  protected readonly list = new ListState<Recipient>({
    pageSize: 10,
    sort: 'createdOn',
    dir: 'asc',
    load: query => this._api.getCampaignMessages(this._campaignId, query.page, query.pageSize,
      query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined)
  });

  public ngOnInit(): void {
    this.list.start();
  }
}
