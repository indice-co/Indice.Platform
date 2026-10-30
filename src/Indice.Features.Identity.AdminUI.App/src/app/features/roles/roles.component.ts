import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { LucideChevronRight, LucidePlus, LucideShieldCheck } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, RoleInfo } from 'src/app/core/services/identity-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState, toApiSort } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Roles list. Creating and editing happen in the side pane. */
@Component({
  selector: 'app-roles',
  imports: [
    RouterLink, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent,
    LucideChevronRight, LucidePlus, LucideShieldCheck, TranslatePipe
  ],
  template: `
    <app-page-header [title]="'Routes.Roles' | translate" [subtitle]="'Roles.List.Subtitle' | translate" [count]="list.total()">
      @if (canEdit) {
        <div actions>
          <a class="btn btn-primary btn-sm" [routerLink]="pane.link('role-add')"><svg lucidePlus class="size-4"></svg>{{ 'Roles.New' | translate }}</a>
        </div>
      }
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Roles.List.SearchPlaceholder' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th app-sort-header [list]="list" field="name">{{ 'Common.Name' | translate }}</th>
            <th>{{ 'Common.Description' | translate }}</th>
            <th class="w-12"><span class="sr-only">{{ 'Roles.List.Open' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="3"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (role of list.items(); track role.id) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  <a class="flex items-center gap-3 font-semibold hover:text-primary" [routerLink]="pane.link(['role', role.id!])">
                    <span class="grid size-8 shrink-0 place-items-center rounded-field bg-primary/10 text-primary">
                      <svg lucideShieldCheck class="size-4"></svg>
                    </span>
                    {{ role.name }}
                  </a>
                </td>
                <td class="max-w-xl text-base-content/75">{{ role.description || '—' }}</td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['role', role.id!])" [attr.aria-label]="'Roles.List.OpenItem' | translate: { name: role.name }">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="3">
                  <app-empty-state [title]="(list.hasFilters() ? 'Roles.List.NoMatches' : 'Roles.List.Empty') | translate"
                    [message]="(list.hasFilters() ? 'Roles.List.NoMatchesHint' : 'Roles.List.EmptyHint') | translate">
                    <svg icon lucideShieldCheck class="size-5"></svg>
                    @if (canEdit && !list.hasFilters()) {
                      <a actions class="btn btn-sm" [routerLink]="pane.link('role-add')"><svg lucidePlus class="size-4"></svg>{{ 'Roles.New' | translate }}</a>
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
export class RolesComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  protected readonly pane = inject(SidePaneService);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();

  protected readonly list = new ListState<RoleInfo>({
    sort: 'name',
    dir: 'asc',
    load: query => this._api.getRoles(query.page, query.pageSize, toApiSort(query), query.search || undefined)
  });

  constructor() {
    inject(RefreshBus).on('roles').pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }
}
