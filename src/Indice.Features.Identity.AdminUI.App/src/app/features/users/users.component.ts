import { Component, OnInit, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideChevronRight, LucideListFilter, LucideUserPlus, LucideUsers, LucideX } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, UserInfo } from 'src/app/core/services/identity-api.service';
import { UiFeaturesService } from 'src/app/core/services/ui-features.service';
import { ImgUserPictureDirective } from 'src/app/ui/avatar/user-picture.directive';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState, toApiSort, toBool } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { STATUS_FILTERS, StatusFilter } from './user-utils';

/** Refresh bus scope for the users list. */
export const USERS_REFRESH_SCOPE = 'users';

@Component({
  selector: 'app-users',
  imports: [
    DatePipe, RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent,
    StatusBadgeComponent, ImgUserPictureDirective, LucideChevronRight, LucideListFilter, LucideUserPlus, LucideUsers, LucideX
  ],
  template: `
    <app-page-header [title]="'Routes.Users' | translate" [subtitle]="'Users.List.Subtitle' | translate">
      @if (canEdit) {
        <div actions>
          <a class="btn btn-primary btn-sm" routerLink="/users/add"><svg lucideUserPlus class="size-4"></svg>{{ 'Routes.NewUser' | translate }}</a>
        </div>
      }
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Users.List.SearchPlaceholder' | translate" (search)="list.setSearch($event)" />

      <div class="dropdown">
        <button type="button" tabindex="0" class="btn btn-sm btn-ghost border-base-300 gap-1.5">
          <svg lucideListFilter class="size-4"></svg>{{ 'Common.Status' | translate }}
          @if (activeFilters().length) { <span class="badge badge-xs badge-primary tabular">{{ activeFilters().length }}</span> }
        </button>
        <div tabindex="0" class="dropdown-content z-30 mt-2 w-80 rounded-box border border-base-300 bg-base-100 p-3 shadow-lg">
          <p class="mb-2 text-xs font-semibold tracking-wide text-base-content/60 uppercase">{{ 'Users.List.FilterByStatus' | translate }}</p>
          <ul class="space-y-2">
            @for (filter of filters; track filter.param) {
              <li class="flex items-center justify-between gap-3">
                <span class="text-sm">{{ filter.label | translate }}</span>
                <div class="join">
                  @for (option of options; track option.value) {
                    <button type="button" class="join-item btn btn-xs"
                      [class.btn-primary]="(list.params()[filter.param] ?? '') === option.value"
                      [class.btn-ghost]="(list.params()[filter.param] ?? '') !== option.value"
                      (click)="setFilter(filter.param, option.value)">{{ option.label | translate }}</button>
                  }
                </div>
              </li>
            }
          </ul>
        </div>
      </div>

      @for (active of activeFilters(); track active.filter.param) {
        <span class="badge badge-soft badge-primary gap-1 py-3">
          {{ (active.value ? active.filter.yes : active.filter.no) | translate }}
          <button type="button" class="btn btn-ghost btn-xs btn-circle -mr-1 size-5 min-h-0" [attr.aria-label]="'Users.List.RemoveFilter' | translate: { name: (active.filter.label | translate) }"
            (click)="setFilter(active.filter.param, null)">
            <svg lucideX class="size-3"></svg>
          </button>
        </span>
      }
      @if (list.hasFilters()) {
        <button type="button" class="btn btn-ghost btn-sm" (click)="list.clearFilters()">{{ 'Users.List.ClearAll' | translate }}</button>
      }
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th><app-sort-header [list]="list" field="userName">{{ 'Routes.User' | translate }}</app-sort-header></th>
            <th class="hidden lg:table-cell">{{ 'Users.List.ColumnPhone' | translate }}</th>
            <th>{{ 'Common.Status' | translate }}</th>
            <th class="hidden md:table-cell"><app-sort-header [list]="list" field="createDate">{{ 'Common.CreatedAt' | translate }}</app-sort-header></th>
            <th class="hidden md:table-cell"><app-sort-header [list]="list" field="lastSignInDate">{{ 'Users.Fields.LastSignIn' | translate }}</app-sort-header></th>
            <th class="w-12"><span class="sr-only">{{ 'Users.List.Open' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4, 5]; track row) {
              <tr><td colspan="6"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (user of list.items(); track user.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  <a class="flex min-w-0 items-center gap-3" [routerLink]="['/users', user.id]">
                    <span class="avatar">
                      <span class="size-9 overflow-hidden rounded-full bg-base-300">
                        <img [userPicture]="user.id!" [displayName]="$any(user)" [size]="64" alt="" />
                      </span>
                    </span>
                    <span class="min-w-0">
                      <span class="block truncate font-semibold hover:text-primary">{{ fullName(user) || user.userName || user.email }}</span>
                      <span class="block truncate text-xs text-base-content/65">
                        {{ user.email }}
                        @if (showUserName() && user.userName && user.userName !== user.email) { · {{ user.userName }} }
                      </span>
                    </span>
                  </a>
                </td>
                <td class="hidden tabular whitespace-nowrap text-base-content/75 lg:table-cell">{{ user.phoneNumber || '—' }}</td>
                <td>
                  <div class="flex flex-wrap gap-1">
                    @if (user.blocked) { <app-status-badge tone="error">{{ 'Users.Status.Blocked' | translate }}</app-status-badge> }
                    @if (user.isLocked) { <app-status-badge tone="warning">{{ 'Users.Status.Locked' | translate }}</app-status-badge> }
                    @if (user.isAdmin) { <app-status-badge tone="primary">{{ 'Users.Status.Admin' | translate }}</app-status-badge> }
                    @if (user.twoFactorEnabled) { <app-status-badge tone="success">{{ 'Users.Status.Mfa' | translate }}</app-status-badge> }
                    @if (!user.emailConfirmed) { <app-status-badge tone="warning">{{ 'Users.Status.EmailUnverified' | translate }}</app-status-badge> }
                    @if (user.phoneNumber && !user.phoneNumberConfirmed) { <app-status-badge tone="neutral">{{ 'Users.Status.PhoneUnverified' | translate }}</app-status-badge> }
                    @if (!user.blocked && !user.isLocked && !user.isAdmin && !user.twoFactorEnabled && user.emailConfirmed && (!user.phoneNumber || user.phoneNumberConfirmed)) {
                      <app-status-badge tone="success">{{ 'Users.Status.Active' | translate }}</app-status-badge>
                    }
                  </div>
                </td>
                <td class="hidden tabular whitespace-nowrap text-base-content/75 md:table-cell">{{ user.createDate | date: 'mediumDate' }}</td>
                <td class="hidden tabular whitespace-nowrap text-base-content/75 md:table-cell">
                  {{ user.lastSignInDate ? (user.lastSignInDate | date: 'short') : ('Common.Never' | translate) }}
                </td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/users', user.id]" [attr.aria-label]="'Users.List.OpenUser' | translate: { name: user.email || user.userName }">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6">
                  <app-empty-state [title]="(list.hasFilters() ? 'Users.List.EmptyFilteredTitle' : 'Users.List.EmptyTitle') | translate"
                    [message]="(list.hasFilters() ? 'Users.List.EmptyFilteredMessage' : 'Users.List.EmptyMessage') | translate">
                    <svg icon lucideUsers class="size-5"></svg>
                    @if (list.hasFilters()) {
                      <button actions type="button" class="btn btn-sm" (click)="list.clearFilters()">{{ 'Users.List.ClearFilters' | translate }}</button>
                    } @else if (canEdit) {
                      <a actions class="btn btn-sm" routerLink="/users/add"><svg lucideUserPlus class="size-4"></svg>{{ 'Routes.NewUser' | translate }}</a>
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
export class UsersComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _features = inject(UiFeaturesService);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  protected readonly filters = STATUS_FILTERS;
  protected readonly options = [{ value: '', label: 'Users.List.Any' }, { value: 'true', label: 'Common.Yes' }, { value: 'false', label: 'Common.No' }];
  /** With `EmailAsUserName` the username is the email, so it is not repeated. */
  protected readonly showUserName = computed(() => !this._features.features()?.emailAsUserName);

  protected readonly list = new ListState<UserInfo>({
    sort: 'createDate',
    dir: 'desc',
    params: STATUS_FILTERS.map(f => f.param),
    load: query => this._api.getUsers(
      query.page, query.pageSize, toApiSort(query), query.search || undefined, undefined, undefined, undefined,
      toBool(query.params['blocked']), toBool(query.params['lockedOut']), toBool(query.params['emailConfirmed']),
      toBool(query.params['phoneNumberConfirmed']), toBool(query.params['twoFactorEnabled']), toBool(query.params['admin'])
    )
  });

  protected readonly activeFilters = computed(() => {
    const params = this.list.params();
    return STATUS_FILTERS
      .filter(filter => toBool(params[filter.param]) !== undefined)
      .map(filter => ({ filter, value: toBool(params[filter.param])! as boolean })) as { filter: StatusFilter; value: boolean }[];
  });

  constructor() {
    inject(RefreshBus).on(USERS_REFRESH_SCOPE).pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }

  protected setFilter(param: string, value: string | null): void {
    this.list.setParams({ [param]: value });
  }

  protected fullName(user: UserInfo): string {
    return `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
  }
}
