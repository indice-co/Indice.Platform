import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideActivity, LucideChevronRight, LucideGauge, LucideRefreshCw, LucideTriangleAlert } from '@lucide/angular';
import { DbAggregateRuleExecutionResult, RiskApiService } from 'src/app/core/services/risk-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { FilterBarComponent } from 'src/app/ui/list/filter-bar.component';
import { FilterClause, FilterField } from 'src/app/ui/list/filter-clause';
import { ListState, toApiSort } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { RiskLevelBadgeComponent } from 'src/app/ui/risk/risk-level-badge.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

@Component({
  selector: 'app-risk-results',
  imports: [
    DatePipe, DecimalPipe, RouterLink, PageHeaderComponent, FilterBarComponent, PaginationComponent, SortHeaderComponent, EmptyStateComponent,
    RiskLevelBadgeComponent, LucideActivity, LucideChevronRight, LucideGauge, LucideRefreshCw, LucideTriangleAlert
  ],
  template: `
    <app-page-header title="Risk results" subtitle="The outcome of every risk evaluation, with the score each rule contributed." />

    <section class="panel">
      <app-filter-bar class="border-b border-base-300 p-4" [fields]="fields" [filters]="list.filters()" (filtersChange)="list.setFilters($event)">
        <button actions type="button" class="btn btn-sm btn-ghost btn-square" aria-label="Refresh" title="Refresh" (click)="list.refresh()">
          <svg lucideRefreshCw class="size-4" [class.animate-spin]="list.loading()"></svg>
        </button>
      </app-filter-bar>

      @if (list.failed()) {
        <app-empty-state title="Risk results could not be loaded" message="The service did not respond as expected. Try again in a moment.">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="list.refresh()">Try again</button>
        </app-empty-state>
      } @else if (list.items() === null) {
        <div class="space-y-3 p-4">
          @for (row of [1, 2, 3, 4, 5, 6]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else if (list.isEmpty()) {
        <app-empty-state title="No risk results" [message]="list.hasFilters() ? 'No results match the current filters.' : 'Results appear here once risk is evaluated.'">
          <svg icon lucideGauge class="size-6"></svg>
          @if (list.hasFilters()) {
            <button actions type="button" class="btn btn-sm" (click)="list.setFilters([])">Clear filters</button>
          }
        </app-empty-state>
      } @else {
        <div class="overflow-x-auto" [class.opacity-60]="list.loading()">
          <table class="table">
            <thead>
              <tr class="text-xs text-base-content/60">
                <th><app-sort-header field="createdAt" label="Evaluated" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th><app-sort-header field="riskScore" label="Risk" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th><app-sort-header field="name" label="Event" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th>Subject</th>
                <th class="text-right"><app-sort-header field="amount" label="Amount" [sort]="list.sort()" [dir]="list.dir()" (sortChange)="list.setSort($event)" /></th>
                <th class="hidden text-right lg:table-cell">Rules</th>
                <th class="hidden xl:table-cell">IP address</th>
                <th><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (item of list.items(); track item.id) {
                <tr class="cursor-pointer hover:bg-base-200/60" (click)="pane.open(['result', item.id!])">
                  <td class="whitespace-nowrap">
                    <span class="block font-medium tabular">{{ item.createdAt | date: 'dd MMM yyyy' }}</span>
                    <span class="block text-xs text-base-content/60 tabular">{{ item.createdAt | date: 'HH:mm:ss' }}</span>
                  </td>
                  <td class="whitespace-nowrap">
                    <app-risk-level-badge [level]="item.riskLevel" />
                    <span class="ml-2 text-sm font-semibold tabular">{{ item.riskScore ?? '—' }}</span>
                  </td>
                  <td>
                    <span class="block font-medium">{{ item.name || '—' }}</span>
                    @if (item.type) { <span class="badge badge-ghost badge-sm mt-0.5">{{ item.type }}</span> }
                  </td>
                  <td class="font-mono text-xs">{{ item.subjectId || '—' }}</td>
                  <td class="text-right font-medium whitespace-nowrap tabular">{{ item.amount != null ? (item.amount | number: '1.2-2') : '—' }}</td>
                  <td class="hidden text-right tabular lg:table-cell">{{ item.numberOfRulesExecuted ?? '—' }}</td>
                  <td class="hidden font-mono text-xs xl:table-cell">{{ item.ipAddress || '—' }}</td>
                  <td class="w-0 whitespace-nowrap text-right">
                    @if (item.eventId) {
                      <a class="btn btn-ghost btn-sm btn-square" routerLink="/risk-events" [queryParams]="{ filter: 'id::eq::' + item.eventId }"
                        aria-label="Show the evaluated event" title="Show the evaluated event" (click)="$event.stopPropagation()">
                        <svg lucideActivity class="size-4"></svg>
                      </a>
                    }
                    <a class="btn btn-ghost btn-sm btn-square" [routerLink]="pane.link(['result', item.id!])" aria-label="View result details"
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
export class RiskResultsComponent {
  private readonly _api = inject(RiskApiService);
  protected readonly pane = inject(SidePaneService);
  protected readonly fields: FilterField[] = [
    { field: 'subjectId', label: 'Subject ID' },
    { field: 'name', label: 'Event name' },
    { field: 'type', label: 'Event type' },
    { field: 'ipAddress', label: 'IP address' },
    { field: 'id', label: 'Result ID', operators: ['eq', 'neq'] }
  ];
  protected readonly list = new ListState<DbAggregateRuleExecutionResult>({
    pageSize: 50,
    sort: 'createdAt',
    dir: 'desc',
    load: query => this._api.getRiskResults(FilterClause.toApiTerms(query.filters), query.page, query.pageSize, toApiSort(query))
  });
}
