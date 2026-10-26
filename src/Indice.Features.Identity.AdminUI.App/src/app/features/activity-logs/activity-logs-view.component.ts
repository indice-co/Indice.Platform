import { Component, OnInit, inject, input, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { LucideActivity, LucideFlag, LucideRotateCcw, LucideSlidersHorizontal } from '@lucide/angular';
import { ActivityLogEntry, IdentityApiService, SubjectFilterMode } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState, toApiSort, toBool } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { LogDeviceComponent, LogIpComponent, LogTimeComponent, toDate, toDateInput } from '../sign-in-logs/log-parts';
import { ActivityLogDetailsComponent } from './activity-log-details.component';

/** Query params the activity log list reacts to (besides paging, sorting and `search`). */
const PARAMS = ['succeeded', 'from', 'to', 'review', 'subject', 'subjectMode', 'action', 'category', 'resourceType', 'resourceId', 'application'];
const ADVANCED = ['subject', 'action', 'category', 'resourceType', 'resourceId', 'application'];

/**
 * Filterable activity log table. With `subject` set (a user id) it lists that user's entries only (as actor, as
 * affected resource, or both) and hides the subject column and filter. Clicking a row opens the entry details.
 */
@Component({
  selector: 'app-activity-logs-view',
  imports: [
    StatusBadgeComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent,
    LogDeviceComponent, LogIpComponent, LogTimeComponent, LucideActivity, LucideFlag, LucideRotateCcw, LucideSlidersHorizontal
  ],
  host: { class: 'block' },
  template: `
    <div class="mb-3 flex flex-wrap items-end gap-2">
      <app-search-input class="w-full sm:w-64" [value]="list.search()" placeholder="Search activity" (search)="list.setSearch($event)" />
      <select class="select select-sm w-36" aria-label="Outcome" [value]="list.params()['succeeded'] ?? ''"
        (change)="list.setParams({ succeeded: $any($event.target).value })">
        <option value="">All outcomes</option>
        <option value="true">Succeeded</option>
        <option value="false">Failed</option>
      </select>
      <label class="input input-sm w-40" title="From date">
        <span class="text-base-content/55">From</span>
        <input type="date" [value]="dateValue('from')" (change)="list.setParams({ from: $any($event.target).value })" />
      </label>
      <label class="input input-sm w-40" title="To date">
        <span class="text-base-content/55">To</span>
        <input type="date" [value]="dateValue('to')" (change)="list.setParams({ to: $any($event.target).value })" />
      </label>
      @if (subject() || list.params()['subject']) {
        <select class="select select-sm w-44" aria-label="Match subject as" [value]="list.params()['subjectMode'] ?? 'Actor'"
          (change)="list.setParams({ subjectMode: $any($event.target).value === 'Actor' ? null : $any($event.target).value })">
          <option value="Actor">Subject is the actor</option>
          <option value="Resource">Subject is the resource</option>
          <option value="Any">Actor or resource</option>
        </select>
      }
      <label class="label h-8 cursor-pointer gap-2 rounded-field border border-base-300 bg-base-100 px-3 text-sm text-base-content">
        <input type="checkbox" class="toggle toggle-xs toggle-warning" [checked]="list.params()['review'] === 'true'"
          (change)="list.setParams({ review: $any($event.target).checked ? 'true' : null })" />
        Marked for review
      </label>
      <button type="button" class="btn btn-ghost btn-sm" [class.btn-active]="showAdvanced()" (click)="showAdvanced.set(!showAdvanced())"
        [attr.aria-expanded]="showAdvanced()">
        <svg lucideSlidersHorizontal class="size-4"></svg>More filters
        @if (advancedCount()) { <span class="badge badge-xs badge-primary">{{ advancedCount() }}</span> }
      </button>
      @if (list.hasFilters()) {
        <button type="button" class="btn btn-ghost btn-sm" (click)="list.clearFilters()"><svg lucideRotateCcw class="size-4"></svg>Reset</button>
      }
    </div>
    @if (showAdvanced()) {
      <div class="mb-3 grid gap-2 rounded-box border border-base-300 bg-base-100 p-3 sm:grid-cols-2 lg:grid-cols-3">
        @if (!subject()) {
          <input class="input input-sm w-full" placeholder="Subject (user id or name)" aria-label="Subject" [value]="list.params()['subject'] ?? ''"
            (change)="list.setParams({ subject: $any($event.target).value.trim() })" />
        }
        <input class="input input-sm w-full" placeholder="Action name" aria-label="Action name" [value]="list.params()['action'] ?? ''"
          (change)="list.setParams({ action: $any($event.target).value.trim() })" />
        <input class="input input-sm w-full" placeholder="Category" aria-label="Category" [value]="list.params()['category'] ?? ''"
          (change)="list.setParams({ category: $any($event.target).value.trim() })" />
        <input class="input input-sm w-full" placeholder="Resource type" aria-label="Resource type" [value]="list.params()['resourceType'] ?? ''"
          (change)="list.setParams({ resourceType: $any($event.target).value.trim() })" />
        <input class="input input-sm w-full font-mono" placeholder="Resource id" aria-label="Resource id" [value]="list.params()['resourceId'] ?? ''"
          (change)="list.setParams({ resourceId: $any($event.target).value.trim() })" />
        <input class="input input-sm w-full font-mono" placeholder="Client id" aria-label="Application id" [value]="list.params()['application'] ?? ''"
          (change)="list.setParams({ application: $any($event.target).value.trim() })" />
      </div>
    }

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th><app-sort-header [list]="list" field="createdAt">When</app-sort-header></th>
            @if (!subject()) { <th><app-sort-header [list]="list" field="subjectName">Subject</app-sort-header></th> }
            <th><app-sort-header [list]="list" field="actionName">Action</app-sort-header></th>
            <th>Description</th>
            <th><app-sort-header [list]="list" field="category">Category</app-sort-header></th>
            <th><app-sort-header [list]="list" field="resourceType">Resource</app-sort-header></th>
            <th>Application</th>
            <th><app-sort-header [list]="list" field="ipAddress">IP address</app-sort-header></th>
            <th>Device</th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4, 5]; track row) {
              <tr><td [attr.colspan]="columns()"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (entry of list.items(); track entry.id) {
              <tr class="cursor-pointer hover:bg-base-200/60" [class.opacity-60]="list.loading()" tabindex="0"
                (click)="open(entry)" (keydown.enter)="open(entry)">
                <td><app-log-time [value]="entry.createdAt" /></td>
                @if (!subject()) {
                  <td class="max-w-44 truncate" [class.text-error]="entry.subjectUnknown" [title]="entry.subjectName ?? ''">{{ entry.subjectName || '—' }}</td>
                }
                <td>
                  <div class="flex items-center gap-2">
                    <app-status-badge [tone]="entry.succeeded ? 'success' : 'error'">{{ entry.actionName || (entry.succeeded ? 'Succeeded' : 'Failed') }}</app-status-badge>
                    @if (entry.review) { <svg lucideFlag class="size-3.5 text-warning" aria-label="Marked for review"></svg> }
                  </div>
                </td>
                <td class="max-w-80 truncate text-base-content/75" [title]="entry.description ?? ''">{{ entry.description || '—' }}</td>
                <td class="whitespace-nowrap">{{ entry.category || '—' }}</td>
                <td class="max-w-56">
                  <div class="flex min-w-0 flex-col leading-tight">
                    <span class="truncate text-sm">{{ entry.resourceType || '—' }}</span>
                    @if (entry.resourceId) { <span class="truncate font-mono text-xs text-base-content/55" [title]="entry.resourceId">{{ entry.resourceId }}</span> }
                  </div>
                </td>
                <td class="max-w-44 truncate" [title]="entry.applicationId ?? ''">{{ entry.applicationName || entry.applicationId || '—' }}</td>
                <td><app-log-ip [ip]="entry.ipAddress" [country]="entry.countryIsoCode" [location]="entry.location" /></td>
                <td class="max-w-56">
                  <app-log-device [displayName]="entry.extraData?.device?.displayName" [userAgentFamily]="entry.extraData?.device?.userAgentFamily"
                    [platform]="entry.extraData?.device?.platform" />
                </td>
              </tr>
            } @empty {
              <tr>
                <td [attr.colspan]="columns()">
                  <app-empty-state [title]="list.failed() ? 'Activity logs could not be loaded' : 'No activity'"
                    [message]="list.hasFilters() ? 'No entries match the current filters.' : 'Changes and actions performed through the identity server will appear here.'">
                    <svg icon lucideActivity class="size-5"></svg>
                    @if (list.hasFilters()) {
                      <button actions type="button" class="btn btn-sm" (click)="list.clearFilters()">Reset filters</button>
                    }
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
export class ActivityLogsViewComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _route = inject(ActivatedRoute);

  /** A user id; limits the list to entries about that user. */
  public readonly subject = input<string>();

  protected readonly list = new ListState<ActivityLogEntry>({
    sort: 'createdAt',
    dir: 'desc',
    params: PARAMS,
    load: query => {
      const subject = this.subject() || query.params['subject'] || undefined;
      return this._api.getActivityLogs(
        query.page,
        query.pageSize,
        toApiSort(query),
        query.search || undefined,
        subject,
        undefined,
        toBool(query.params['review']),
        toBool(query.params['succeeded']),
        query.params['action'] || undefined,
        query.params['resourceId'] || undefined,
        query.params['resourceType'] || undefined,
        query.params['category'] || undefined,
        subject ? (query.params['subjectMode'] as SubjectFilterMode) || SubjectFilterMode.Actor : undefined,
        toDate(query.params['from']),
        toDate(query.params['to']),
        query.params['application'] || undefined
      );
    }
  });

  protected readonly showAdvanced = signal(false);

  protected advancedCount(): number {
    const params = this.list.params();
    return ADVANCED.filter(key => !!params[key]).length;
  }

  protected columns(): number {
    return this.subject() ? 8 : 9;
  }

  public ngOnInit(): void {
    this.showAdvanced.set(this.advancedCountFromUrl() > 0);
    this.list.start();
  }

  protected dateValue(param: string): string {
    return toDateInput(this.list.params()[param]);
  }

  protected open(entry: ActivityLogEntry): void {
    this._dialog.open<boolean, ActivityLogEntry>(ActivityLogDetailsComponent, { data: entry, size: 'xl' }).closed.subscribe(changed => {
      if (changed) {
        this.list.refresh();
      }
    });
  }

  private advancedCountFromUrl(): number {
    const params = this._route.snapshot.queryParamMap;
    return ADVANCED.filter(key => !!params.get(key)).length;
  }
}
