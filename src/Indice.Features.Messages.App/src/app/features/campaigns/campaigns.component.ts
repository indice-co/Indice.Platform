import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowDown, LucideArrowUp, LucideArrowUpDown, LucideMegaphone, LucidePlus } from '@lucide/angular';
import { Campaign, MessagesApiClient, MessageType } from 'src/app/core/services/messages-api.service';
import { CHANNEL_KINDS, ChannelIconComponent, MessageTypeBadgeComponent } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterClause } from 'src/app/ui/list/filter-clause';
import { ListState } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

const TYPE_FILTER = 'type.id';
const PUBLISHED_FILTER = 'published';

@Component({
  selector: 'app-campaigns',
  imports: [
    DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent,
    ChannelIconComponent, MessageTypeBadgeComponent, LucideArrowDown, LucideArrowUp, LucideArrowUpDown, LucideMegaphone, LucidePlus
  ],
  template: `
    <app-page-header [title]="'Campaigns.Title' | translate" [subtitle]="'Campaigns.Subtitle' | translate">
      <div actions>
        <a class="btn btn-primary btn-sm" routerLink="add-campaign">
          <svg lucidePlus class="size-4"></svg>{{ 'Campaigns.CreateCampaignAction' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Shared.Search' | translate" (search)="list.setSearch($event)" />
      <select class="select select-sm sm:w-56" [attr.aria-label]="'Campaigns.TypeLabel' | translate" (change)="setFilter(typeFilter, $any($event.target).value)">
        <option value="" [selected]="!typeId()">{{ 'Campaigns.AllTypesOption' | translate }}</option>
        @for (type of messageTypes(); track type.id) {
          <option [value]="type.id" [selected]="type.id === typeId()">{{ type.name }}</option>
        }
      </select>
      <div class="join" role="group" [attr.aria-label]="'Campaigns.StatusLabel' | translate">
        @for (option of statusOptions; track option.value) {
          <button type="button" class="join-item btn btn-sm" [class.btn-active]="published() === option.value"
            (click)="setFilter(publishedFilter, option.value)">{{ option.label | translate }}</button>
        }
      </div>
    </div>

    <div class="panel overflow-x-auto">
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
            @for (row of [1, 2, 3, 4, 5]; track row) {
              <tr><td [attr.colspan]="columns.length"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (item of list.items(); track item.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td class="min-w-56">
                  <a class="link link-hover font-semibold" [routerLink]="[item.id]">{{ item.title }}</a>
                  @if (item.createdBy) {
                    <p class="text-xs text-base-content/60">{{ item.createdBy }}</p>
                  }
                </td>
                <td>
                  <div class="flex items-center gap-1">
                    @for (kind of channelKinds; track kind) {
                      <app-channel-icon [kind]="kind" [muted]="!item.messageChannelKind?.includes($any(kind))" />
                    }
                  </div>
                </td>
                <td><app-message-type-badge [name]="item.type?.name" [classification]="item.type?.classification" /></td>
                <td class="whitespace-nowrap text-sm tabular">
                  @if (item.activePeriod?.from || item.activePeriod?.to) {
                    {{ item.activePeriod?.from ? (item.activePeriod?.from | date: 'mediumDate') : '' }} –
                    {{ item.activePeriod?.to ? (item.activePeriod?.to | date: 'mediumDate') : ('Campaigns.InfiniteSymbol' | translate) }}
                  } @else {
                    <span class="text-base-content/40">—</span>
                  }
                </td>
                <td>
                  @if (item.published) {
                    <span class="badge badge-sm badge-soft badge-success whitespace-nowrap">{{ 'Campaigns.StatusPublished' | translate }}</span>
                  } @else {
                    <span class="badge badge-sm badge-ghost whitespace-nowrap">{{ 'Campaigns.StatusDraft' | translate }}</span>
                  }
                </td>
                <td class="whitespace-nowrap text-sm text-base-content/70 tabular">{{ item.createdAt | date: 'medium' }}</td>
              </tr>
            } @empty {
              <tr>
                <td [attr.colspan]="columns.length">
                  <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Shared.ListViewEmptyStateSubTitle' | translate">
                    <svg icon lucideMegaphone class="size-5"></svg>
                    <a actions class="btn btn-sm" routerLink="add-campaign">
                      <svg lucidePlus class="size-4"></svg>{{ 'Campaigns.CreateCampaignAction' | translate }}
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
export class CampaignsComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);

  protected readonly typeFilter = TYPE_FILTER;
  protected readonly publishedFilter = PUBLISHED_FILTER;
  protected readonly channelKinds = CHANNEL_KINDS;
  protected readonly messageTypes = signal<MessageType[]>([]);
  protected readonly statusOptions = [
    { value: '', label: 'Campaigns.AllStatusesOption' },
    { value: 'true', label: 'Campaigns.StatusPublished' },
    { value: 'false', label: 'Campaigns.StatusDraft' }
  ];
  protected readonly columns: { label: string; sort?: string; class?: string }[] = [
    { label: 'Campaigns.ItemTitle', sort: 'title' },
    { label: 'Campaigns.Channels' },
    { label: 'Campaigns.Type', sort: 'type.name' },
    { label: 'Campaigns.ActivePeriodColumn', sort: 'activePeriod.from' },
    { label: 'Campaigns.StatusLabel', sort: 'published' },
    { label: 'Campaigns.CreatedAt', sort: 'createdAt' }
  ];

  protected readonly list = new ListState<Campaign>({
    pageSize: 20,
    sort: 'createdAt',
    dir: 'desc',
    load: query => {
      const typeId = query.filters.find(f => f.member === TYPE_FILTER)?.value;
      const published = query.filters.find(f => f.member === PUBLISHED_FILTER)?.value;
      return this._api.getCampaigns(
        query.page, query.pageSize, query.dir === 'asc' ? query.sort : `${query.sort}-`, query.search || undefined,
        undefined, published === undefined ? undefined : `${published}` === 'true', undefined, typeId ? [typeId] : undefined
      );
    }
  });
  protected readonly typeId = computed(() => this.list.filters().find(f => f.member === TYPE_FILTER)?.value ?? '');
  protected readonly published = computed(() => `${this.list.filters().find(f => f.member === PUBLISHED_FILTER)?.value ?? ''}`);

  constructor() {
    inject(RefreshBus).on('campaigns').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
    this._api.getMessageTypes(1, 100, 'name+').subscribe(result => this.messageTypes.set(result.items ?? []));
  }

  protected setFilter(member: string, value: string): void {
    const filters = this.list.filters().filter(f => f.member !== member);
    if (value) {
      filters.push(new FilterClause(member, value, 'eq', member === PUBLISHED_FILTER ? 'boolean' : 'string'));
    }
    this.list.setFilters(filters);
  }
}
