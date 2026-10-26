import { Component, OnInit, inject, input } from '@angular/core';
import { LucideFlag, LucideLogIn, LucideRotateCcw } from '@lucide/angular';
import { IdentityApiService, SignInLogEntry, SignInType } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState, toApiSort, toBool } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { LogDeviceComponent, LogIpComponent, LogTimeComponent, idpIcon, toDate, toDateInput } from './log-parts';
import { SignInLogDetailsComponent } from './sign-in-log-details.component';

/** Query params the sign in log list reacts to (besides paging, sorting and `search`). */
const PARAMS = ['succeeded', 'signInType', 'from', 'to', 'review', 'subject', 'application'];

/**
 * Filterable sign in log table. With `subject` set (a user id) it lists that user's entries only and hides the
 * subject column and filter. Clicking a row opens the entry details.
 */
@Component({
  selector: 'app-sign-in-logs-view',
  imports: [
    StatusBadgeComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent,
    LogDeviceComponent, LogIpComponent, LogTimeComponent, LucideFlag, LucideLogIn, LucideRotateCcw
  ],
  host: { class: 'block' },
  template: `
    <div class="mb-3 flex flex-wrap items-end gap-2">
      <app-search-input class="w-full sm:w-64" [value]="list.search()" placeholder="Search logs" (search)="list.setSearch($event)" />
      <select class="select select-sm w-36" aria-label="Outcome" [value]="list.params()['succeeded'] ?? ''"
        (change)="list.setParams({ succeeded: $any($event.target).value })">
        <option value="">All outcomes</option>
        <option value="true">Succeeded</option>
        <option value="false">Failed</option>
      </select>
      <select class="select select-sm w-40" aria-label="Sign-in type" [value]="list.params()['signInType'] ?? ''"
        (change)="list.setParams({ signInType: $any($event.target).value })">
        <option value="">All sign-in types</option>
        @for (type of signInTypes; track type) { <option [value]="type" [selected]="list.params()['signInType'] === type">{{ type === 'NonInteractive' ? 'Non-interactive' : type }}</option> }
      </select>
      <label class="input input-sm w-40" title="From date">
        <span class="text-base-content/55">From</span>
        <input type="date" [value]="dateValue('from')" (change)="list.setParams({ from: $any($event.target).value })" />
      </label>
      <label class="input input-sm w-40" title="To date">
        <span class="text-base-content/55">To</span>
        <input type="date" [value]="dateValue('to')" (change)="list.setParams({ to: $any($event.target).value })" />
      </label>
      @if (!subject()) {
        <input class="input input-sm w-40" placeholder="Subject" aria-label="Subject" [value]="list.params()['subject'] ?? ''"
          (change)="list.setParams({ subject: $any($event.target).value.trim() })" />
      }
      <input class="input input-sm w-40 font-mono" placeholder="Client id" aria-label="Application id" [value]="list.params()['application'] ?? ''"
        (change)="list.setParams({ application: $any($event.target).value.trim() })" />
      <label class="label h-8 cursor-pointer gap-2 rounded-field border border-base-300 bg-base-100 px-3 text-sm text-base-content">
        <input type="checkbox" class="toggle toggle-xs toggle-warning" [checked]="list.params()['review'] === 'true'"
          (change)="list.setParams({ review: $any($event.target).checked ? 'true' : null })" />
        Marked for review
      </label>
      @if (list.hasFilters()) {
        <button type="button" class="btn btn-ghost btn-sm" (click)="list.clearFilters()"><svg lucideRotateCcw class="size-4"></svg>Reset</button>
      }
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th><app-sort-header [list]="list" field="createdAt">When</app-sort-header></th>
            <th><app-sort-header [list]="list" field="actionName">Event</app-sort-header></th>
            @if (!subject()) { <th><app-sort-header [list]="list" field="subjectName">Subject</app-sort-header></th> }
            <th>Application</th>
            <th>Device</th>
            <th><app-sort-header [list]="list" field="ipAddress">IP address</app-sort-header></th>
            <th><app-sort-header [list]="list" field="location">Location</app-sort-header></th>
            <th><app-sort-header [list]="list" field="signInType">Type</app-sort-header></th>
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
                <td>
                  <div class="flex items-center gap-2">
                    <app-status-badge [tone]="entry.succeeded ? 'success' : 'error'">{{ entry.actionName || (entry.succeeded ? 'Succeeded' : 'Failed') }}</app-status-badge>
                    @if (entry.review) { <svg lucideFlag class="size-3.5 text-warning" aria-label="Marked for review"></svg> }
                    @if (idp(entry); as icon) { <img [src]="icon" class="h-3.5" [alt]="entry.extraData?.provider" [title]="entry.extraData?.provider" /> }
                  </div>
                </td>
                @if (!subject()) {
                  <td class="max-w-48 truncate" [class.text-error]="entry.subjectUnknown" [title]="entry.subjectName ?? ''">{{ entry.subjectName || '—' }}</td>
                }
                <td class="max-w-48 truncate" [title]="entry.applicationId ?? ''">{{ entry.applicationName || entry.applicationId || '—' }}</td>
                <td class="max-w-56">
                  <app-log-device [displayName]="entry.extraData?.device?.displayName" [userAgentFamily]="entry.extraData?.device?.userAgentFamily"
                    [platform]="entry.extraData?.device?.platform" />
                </td>
                <td><app-log-ip [ip]="entry.ipAddress" [country]="entry.countryIsoCode" [location]="entry.location" /></td>
                <td class="max-w-48 truncate text-base-content/75" [title]="entry.location ?? ''">{{ entry.location || '—' }}</td>
                <td>
                  @if (entry.signInType) {
                    <span class="badge badge-sm badge-ghost whitespace-nowrap">{{ entry.signInType === 'NonInteractive' ? 'Non-interactive' : entry.signInType }}</span>
                  } @else { <span class="text-base-content/40">—</span> }
                </td>
              </tr>
            } @empty {
              <tr>
                <td [attr.colspan]="columns()">
                  <app-empty-state [title]="list.failed() ? 'Sign-in logs could not be loaded' : 'No sign-in events'"
                    [message]="list.hasFilters() ? 'No entries match the current filters.' : 'Sign-in events will appear here as users authenticate.'">
                    <svg icon lucideLogIn class="size-5"></svg>
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
export class SignInLogsViewComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);

  /** A user id; limits the list to that user's sign ins. */
  public readonly subject = input<string>();

  protected readonly signInTypes = Object.values(SignInType);
  protected readonly list = new ListState<SignInLogEntry>({
    sort: 'createdAt',
    dir: 'desc',
    params: PARAMS,
    load: query => this._api.getSignInLogs(
      query.page,
      query.pageSize,
      toApiSort(query),
      query.search || undefined,
      this.subject() || query.params['subject'] || undefined,
      undefined,
      toBool(query.params['review']),
      toBool(query.params['succeeded']),
      undefined,
      toDate(query.params['from']),
      toDate(query.params['to']),
      query.params['application'] || undefined,
      (query.params['signInType'] as SignInType) || undefined
    )
  });

  protected columns(): number {
    return this.subject() ? 7 : 8;
  }

  public ngOnInit(): void {
    this.list.start();
  }

  protected dateValue(param: string): string {
    return toDateInput(this.list.params()[param]);
  }

  protected idp(entry: SignInLogEntry): string | undefined {
    return idpIcon(entry.extraData?.provider);
  }

  protected open(entry: SignInLogEntry): void {
    this._dialog.open<boolean, SignInLogEntry>(SignInLogDetailsComponent, { data: entry, size: 'xl' }).closed.subscribe(changed => {
      if (changed) {
        this.list.refresh();
      }
    });
  }
}
