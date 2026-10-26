import { Component, OnInit, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, map } from 'rxjs';
import { LucideBoxes, LucideChevronRight, LucideIdCard, LucideKeyRound, LucideLock, LucidePlus } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListQuery, ListState, ResultSet, toApiSort } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

export type ResourceKind = 'identity' | 'api' | 'scope';

/** The fields the three resource lists have in common. */
interface ResourceRow {
  id?: number;
  name?: string;
  displayName?: string;
  description?: string;
  enabled?: boolean;
  required?: boolean;
  emphasize?: boolean;
  nonEditable?: boolean;
  userClaims?: string[];
  allowedClaims?: string[];
}

interface KindConfig {
  title: string;
  subtitle: string;
  noun: string;
  search: string;
}

const KINDS: Record<ResourceKind, KindConfig> = {
  identity: {
    title: 'Identity resources',
    subtitle: 'Groups of user claims (such as profile or email) that clients can request about the signed-in user.',
    noun: 'identity resource',
    search: 'Search identity resources'
  },
  api: {
    title: 'API resources',
    subtitle: 'APIs protected by the identity server, along with their scopes, claims and secrets.',
    noun: 'API resource',
    search: 'Search API resources'
  },
  scope: {
    title: 'API scopes',
    subtitle: 'Permissions that clients request in order to call APIs on behalf of users.',
    noun: 'API scope',
    search: 'Search API scopes'
  }
};

/** Identity resources, API resources and API scopes lists; `kind` comes from route data. */
@Component({
  selector: 'app-resource-list',
  imports: [
    RouterLink, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent, StatusBadgeComponent,
    LucideBoxes, LucideChevronRight, LucideIdCard, LucideKeyRound, LucideLock, LucidePlus
  ],
  template: `
    <app-page-header [title]="config().title" [subtitle]="config().subtitle">
      @if (canEdit) {
        <div actions>
          <a class="btn btn-primary btn-sm" routerLink="/resources/add" [state]="{ origin: kind() }">
            <svg lucidePlus class="size-4"></svg>New {{ config().noun }}
          </a>
        </div>
      }
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="config().search" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th><app-sort-header [list]="list" field="name">Name</app-sort-header></th>
            <th><app-sort-header [list]="list" field="displayName">Display name</app-sort-header></th>
            <th class="hidden lg:table-cell">Description</th>
            <th>Claims</th>
            <th class="w-12"><span class="sr-only">Open</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="5"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (row of list.items(); track $index) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td>
                  <a class="flex items-center gap-3 hover:text-primary" [routerLink]="link(row)">
                    <span class="grid size-8 shrink-0 place-items-center rounded-field bg-primary/10 text-primary">
                      @switch (kind()) {
                        @case ('identity') { <svg lucideIdCard class="size-4"></svg> }
                        @case ('api') { <svg lucideBoxes class="size-4"></svg> }
                        @default { <svg lucideKeyRound class="size-4"></svg> }
                      }
                    </span>
                    <span class="min-w-0">
                      <span class="flex items-center gap-1.5 font-mono text-sm font-semibold">
                        {{ row.name }}
                        @if (row.nonEditable) { <svg lucideLock class="size-3.5 text-warning" aria-label="Reserved"></svg> }
                      </span>
                      <span class="mt-0.5 flex flex-wrap gap-1">
                        @if (row.enabled === false) { <app-status-badge tone="neutral">Disabled</app-status-badge> }
                        @if (row.required) { <app-status-badge tone="info">Required</app-status-badge> }
                        @if (row.emphasize) { <app-status-badge tone="secondary">Emphasized</app-status-badge> }
                      </span>
                    </span>
                  </a>
                </td>
                <td>{{ row.displayName || '—' }}</td>
                <td class="hidden max-w-md text-base-content/75 lg:table-cell"><span class="line-clamp-2">{{ row.description || '—' }}</span></td>
                <td><span class="badge badge-sm badge-ghost tabular">{{ claimsOf(row).length }}</span></td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="link(row)" [attr.aria-label]="'Open ' + row.name">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5">
                  <app-empty-state [title]="list.hasFilters() ? 'No matches' : 'Nothing here yet'"
                    [message]="list.hasFilters() ? 'Try a different search term.' : 'Create the first ' + config().noun + '.'">
                    <svg icon lucideKeyRound class="size-5"></svg>
                    @if (canEdit && !list.hasFilters()) {
                      <a actions class="btn btn-sm" routerLink="/resources/add" [state]="{ origin: kind() }">
                        <svg lucidePlus class="size-4"></svg>New {{ config().noun }}
                      </a>
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
export class ResourceListComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  /** Bound from route data. */
  public readonly kind = input<ResourceKind>('identity');
  protected readonly config = computed(() => KINDS[this.kind()]);

  protected readonly list = new ListState<ResourceRow>({
    sort: 'name',
    dir: 'asc',
    load: query => this.load(query)
  });

  public ngOnInit(): void {
    this.list.start();
  }

  protected link(row: ResourceRow): string[] {
    switch (this.kind()) {
      case 'identity': return ['/resources/identity', String(row.id)];
      case 'api': return ['/resources/api', String(row.id)];
      default: return ['/resources/scopes', row.name!];
    }
  }

  protected claimsOf(row: ResourceRow): string[] {
    return row.allowedClaims ?? row.userClaims ?? [];
  }

  private load(query: ListQuery): Observable<ResultSet<ResourceRow>> {
    const args = [query.page, query.pageSize, toApiSort(query), query.search || undefined] as const;
    const result$: Observable<ResultSet<any>> = this.kind() === 'identity'
      ? this._api.getIdentityResources(...args)
      : this.kind() === 'api' ? this._api.getApiResources(...args) : this._api.getApiScopes(...args);
    return result$.pipe(map(result => result as ResultSet<ResourceRow>));
  }
}
