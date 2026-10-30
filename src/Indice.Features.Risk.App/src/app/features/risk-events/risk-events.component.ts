import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideActivity, LucideChevronRight, LucideRefreshCw, LucideTriangleAlert } from '@lucide/angular';
import { RiskApiService, RiskEvent } from 'src/app/core/services/risk-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterBarComponent } from 'src/app/ui/list/filter-bar.component';
import { FilterClause, FilterField } from 'src/app/ui/list/filter-clause';
import { ListState, toApiSort } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

@Component({
  selector: 'app-risk-events',
  imports: [
    DatePipe, DecimalPipe, RouterLink, PageHeaderComponent, FilterBarComponent, PaginationComponent, SortHeaderComponent, EmptyStateComponent,
    LucideActivity, LucideChevronRight, LucideRefreshCw, LucideTriangleAlert
  ],
  template: `
    <app-page-header title="Risk events" subtitle="Transactions and user actions reported to the risk engine for evaluation." />

    <section class="panel">
      <app-filter-bar class="border-b border-base-300 p-4" [fields]="fields" [filters]="list.filters()" (filtersChange)="list.setFilters($event)">
        <button actions type="button" class="btn btn-sm btn-ghost btn-square" aria-label="Refresh" title="Refresh" (click)="list.refresh()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="list.loading()"></svg>
        </button>
      </app-filter-bar>

      @if (list.failed()) {
        <app-empty-state title="Risk events could not be loaded" message="The service did not respond as expected. Try again in a moment.">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="list.refresh()">Try again</button>
        </app-empty-state>
      } @else if (list.items() === null) {
        <div class="space-y-3 p-4">
          @for (row of [1, 2, 3, 4, 5, 6]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else if (list.isEmpty()) {
        <app-empty-state title="No risk events" [message]="list.hasFilters() ? 'No events match the current filters.' : 'Events appear here as soon as they are reported.'">
          <svg icon lucideActivity class="size-6"></svg>
          @if (list.hasFilters()) {
            <button actions type="button" class="btn btn-sm" (click)="list.setFilters([])">Clear filters</button>
          }
        </app-empty-state>
      } @else {
        <div class="overflow-x-auto" [class.opacity-60]="list.loading()">
          <table class="table">
            <thead>
              <tr class="text-xs text-base-content/60">
                <th><app-sort-header field="createdAt" label="Created" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th><app-sort-header field="name" label="Event" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th>Subject</th>
                <th class="text-right"><app-sort-header field="amount" label="Amount" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th>Origin</th>
                <th class="hidden xl:table-cell">Source transaction</th>
                <th class="hidden 2xl:table-cell">Session</th>
                <th><span class="sr-only">Details</span></th>
              </tr>
            </thead>
            <tbody>
              @for (item of list.items(); track item.id) {
                <tr class="cursor-pointer hover:bg-base-200/60" (click)="pane.open(['event', item.id!])">
                  <td class="whitespace-nowrap">
                    <span class="block font-medium tabular">{{ item.createdAt | date: 'dd MMM yyyy' }}</span>
                    <span class="block text-xs text-base-content/60 tabular">{{ item.createdAt | date: 'HH:mm:ss' }}</span>
                  </td>
                  <td>
                    <span class="block font-medium">{{ item.name || '—' }}</span>
                    @if (item.type) { <span class="badge badge-ghost badge-sm mt-0.5">{{ item.type }}</span> }
                  </td>
                  <td class="font-mono text-xs">{{ item.subjectId || '—' }}</td>
                  <td class="text-right font-medium whitespace-nowrap tabular">{{ item.amount != null ? (item.amount | number: '1.2-2') : '—' }}</td>
                  <td class="whitespace-nowrap">
                    <span class="font-mono text-xs">{{ item.ipAddress || '—' }}</span>
                    @if (item.countryIsoCode) { <span class="badge badge-outline badge-xs ml-1.5 font-semibold">{{ item.countryIsoCode }}</span> }
                  </td>
                  <td class="hidden max-w-48 truncate font-mono text-xs xl:table-cell" [title]="item.sourceTransId ?? ''">{{ item.sourceTransId || '—' }}</td>
                  <td class="hidden max-w-48 truncate font-mono text-xs 2xl:table-cell" [title]="item.sessionId ?? ''">{{ item.sessionId || '—' }}</td>
                  <td class="w-0 text-right">
                    <a class="btn btn-ghost btn-sm btn-square" [routerLink]="pane.link(['event', item.id!])" aria-label="View event details"
                      (click)="$event.stopPropagation()">
                      <svg lucideChevronRight class="size-4"></svg>
                    </a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination class="border-t border-base-300 px-4 py-3" [page]="list.page()" [pageSize]="list.pageSize()" [count]="list.count() ?? 0"
          (pageChange)="list.setPage($event)" (pageSizeChange)="list.setPageSize($event)" />
      }
    </section>
  `
})
export class RiskEventsComponent {
  private readonly _api = inject(RiskApiService);
  protected readonly pane = inject(SidePaneService);
  protected readonly fields: FilterField[] = [
    { field: 'subjectId', label: 'Subject ID' },
    { field: 'name', label: 'Event name' },
    { field: 'type', label: 'Event type' },
    { field: 'ipAddress', label: 'IP address' },
    { field: 'countryIsoCode', label: 'Country', placeholder: 'e.g. GR' },
    { field: 'location', label: 'Location' },
    { field: 'sourceTransId', label: 'Source transaction ID' },
    { field: 'sessionId', label: 'Session ID' },
    { field: 'id', label: 'Event ID', operators: ['eq', 'neq'] }
  ];
  protected readonly list = new ListState<RiskEvent>({
    pageSize: 50,
    sort: 'createdAt',
    dir: 'desc',
    load: query => this._api.getRiskEvents(FilterClause.toApiTerms(query.filters), query.page, query.pageSize, toApiSort(query))
  });
}
