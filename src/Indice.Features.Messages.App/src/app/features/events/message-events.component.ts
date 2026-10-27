import { Component, OnInit, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideActivity, LucideArrowDown, LucideArrowUp, LucideCircleCheck, LucideCircleX, LucideMail, LucideMegaphone, LucideUser, LucideX } from '@lucide/angular';
import { MessageChannelKind, MessageEvent, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { CHANNEL_KINDS, ChannelIconComponent, ChannelKind } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { ListCountComponent } from 'src/app/ui/list/list-count.component';

/** Translation key and badge style per message event type (`MessageEventType` on the server). */
const EVENT_TYPES: Record<string, { label: string; badge: string }> = {
  Created: { label: 'Events.Types.Created', badge: 'badge-neutral badge-soft' },
  Sent: { label: 'Events.Types.Sent', badge: 'badge-info badge-soft' },
  Delivered: { label: 'Events.Types.Delivered', badge: 'badge-success badge-soft' },
  Opened: { label: 'Events.Types.Opened', badge: 'badge-accent badge-soft' },
  Read: { label: 'Events.Types.Read', badge: 'badge-primary badge-soft' },
  UnRead: { label: 'Events.Types.UnRead', badge: 'badge-ghost' },
  Deleted: { label: 'Events.Types.Deleted', badge: 'badge-warning badge-soft' }
};

@Component({
  selector: 'app-message-events',
  imports: [
    ListCountComponent, DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, ChannelIconComponent,
    LucideActivity, LucideArrowDown, LucideArrowUp, LucideCircleCheck, LucideCircleX, LucideMail, LucideMegaphone, LucideUser, LucideX
  ],
  template: `
    <app-page-header [title]="'Events.Title' | translate" [subtitle]="'Events.Subtitle' | translate">
      <app-list-count class="mt-0.5" [count]="list.items() ? list.count() : null" one="Events.CountOne" other="Events.CountOther" />
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 lg:flex-row lg:flex-wrap lg:items-center">
      <app-search-input class="lg:w-64" [value]="list.search()" [placeholder]="'Events.SearchRecipient' | translate" (search)="list.setSearch($event)" />

      <div class="flex flex-wrap items-center gap-1" role="group" [attr.aria-label]="'Events.Channel' | translate">
        @for (kind of channelKinds; track kind) {
          @let on = channels().includes(kind);
          <button type="button" class="btn btn-sm" [class.btn-active]="on" [class.btn-ghost]="!on" [attr.aria-pressed]="on" (click)="toggleChannel(kind)">
            <app-channel-icon [kind]="kind" [muted]="!on && channels().length > 0" [label]="true" />
          </button>
        }
      </div>

      <div class="join">
        <label class="input input-sm join-item">
          <span class="label">{{ 'Events.From' | translate }}</span>
          <input type="date" [value]="from()" [max]="to() || null" (change)="setDate('from', $any($event.target).value)" />
        </label>
        <label class="input input-sm join-item">
          <span class="label">{{ 'Events.To' | translate }}</span>
          <input type="date" [value]="to()" [min]="from() || null" (change)="setDate('to', $any($event.target).value)" />
        </label>
      </div>

      @if (list.hasFilters()) {
        <button type="button" class="btn btn-ghost btn-sm" (click)="list.update({ filters: [], search: '' })">
          <svg lucideX class="size-4"></svg>{{ 'Events.ClearFilters' | translate }}
        </button>
      }
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>
              <button type="button" class="inline-flex items-center gap-1 uppercase" (click)="list.setSort('recipient')">
                {{ 'Events.Recipient' | translate }}
                @if (list.sort() === 'recipient') {
                  @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
                }
              </button>
            </th>
            <th>{{ 'Events.ItemTitle' | translate }}</th>
            <th>{{ 'Events.Channel' | translate }}</th>
            <th>{{ 'Events.EventType' | translate }}</th>
            <th>{{ 'Events.Success' | translate }}</th>
            <th>
              <button type="button" class="inline-flex items-center gap-1 uppercase" (click)="list.setSort('createdOn')">
                {{ 'Contacts.CreatedAt' | translate }}
                @if (list.sort() === 'createdOn') {
                  @if (list.dir() === 'asc') { <svg lucideArrowUp class="size-3.5"></svg> } @else { <svg lucideArrowDown class="size-3.5"></svg> }
                }
              </button>
            </th>
            <th class="text-right">{{ 'Events.Links' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4, 5]; track row) {
              <tr><td colspan="7"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              @let type = eventType(item.type);
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td class="font-medium break-all">{{ item.recipient || '—' }}</td>
                <td class="max-w-xs truncate" [title]="item.title ?? ''">{{ item.title || '—' }}</td>
                <td class="whitespace-nowrap"><app-channel-icon [kind]="item.channel" [label]="true" /></td>
                <td>
                  <span class="badge badge-sm whitespace-nowrap" [class]="type?.badge ?? 'badge-ghost'">{{ type ? (type.label | translate) : item.type }}</span>
                </td>
                <td>
                  @if (item.success) {
                    <span class="inline-flex items-center gap-1 text-sm text-success"><svg lucideCircleCheck class="size-4"></svg>{{ 'Common.Yes' | translate }}</span>
                  } @else {
                    <span class="inline-flex items-center gap-1 text-sm text-error"><svg lucideCircleX class="size-4"></svg>{{ 'Common.No' | translate }}</span>
                  }
                </td>
                <td class="whitespace-nowrap text-sm">{{ item.createdOn | date: 'medium' }}</td>
                <td class="text-right whitespace-nowrap">
                  <span class="tooltip" [attr.data-tip]="'Contacts.CampaignDetails' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/campaigns', item.campaignId, 'campaign-details']"
                      [attr.aria-label]="'Contacts.CampaignDetails' | translate">
                      <svg lucideMegaphone class="size-4"></svg>
                    </a>
                  </span>
                  <span class="tooltip" [attr.data-tip]="'Events.MessageDetails' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/campaigns', item.campaignId, 'campaign-message-timeline', item.contactId]"
                      [attr.aria-label]="'Events.MessageDetails' | translate">
                      <svg lucideMail class="size-4"></svg>
                    </a>
                  </span>
                  <span class="tooltip tooltip-left" [attr.data-tip]="'Events.ContactDetails' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/contacts', item.contactId, 'contact-details']"
                      [attr.aria-label]="'Events.ContactDetails' | translate">
                      <svg lucideUser class="size-4"></svg>
                    </a>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Events.EmptyMessage' | translate">
                    <svg icon lucideActivity class="size-5"></svg>
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
export class MessageEventsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);

  protected readonly channelKinds = CHANNEL_KINDS;
  protected readonly list = new ListState<MessageEvent>({
    pageSize: 20,
    sort: 'createdOn',
    dir: 'desc',
    load: query => {
      const value = (member: string) => query.filters.find(f => f.member === member)?.value as string | undefined;
      const channels = query.filters.filter(f => f.member === 'channel').map(f => f.value as MessageChannelKind);
      const from = value('from');
      const to = value('to');
      return this._api.getEventsList(
        query.page,
        query.pageSize,
        query.dir === 'asc' ? query.sort : `${query.sort}-`,
        undefined,
        undefined, // campaignId
        undefined, // messageId
        from ? new Date(`${from}T00:00:00`) : undefined,
        to ? new Date(`${to}T23:59:59.999`) : undefined,
        channels.length ? channels : undefined,
        query.search || undefined // recipient
      );
    }
  });

  protected readonly channels = computed(() => this.list.filters().filter(f => f.member === 'channel').map(f => f.value as string));
  protected readonly from = computed(() => this.list.filters().find(f => f.member === 'from')?.value ?? '');
  protected readonly to = computed(() => this.list.filters().find(f => f.member === 'to')?.value ?? '');

  public ngOnInit(): void {
    this.list.start();
  }

  protected eventType(type: string | undefined): { label: string; badge: string } | undefined {
    return type ? EVENT_TYPES[type] : undefined;
  }

  protected toggleChannel(kind: ChannelKind): void {
    const filters = this.list.filters();
    const exists = filters.some(f => f.member === 'channel' && f.value === kind);
    this.list.setFilters(exists
      ? filters.filter(f => !(f.member === 'channel' && f.value === kind))
      : [...filters, new FilterClause('channel', kind)]);
  }

  protected setDate(member: 'from' | 'to', value: string): void {
    const filters = this.list.filters().filter(f => f.member !== member);
    this.list.setFilters(value ? [...filters, new FilterClause(member, value)] : filters);
  }
}
