import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAppWindow, LucideChevronRight, LucideLock, LucidePlus } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClientInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState, toApiSort } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { ClientLogoComponent } from './client-logo.component';

/** Client applications list. */
@Component({
  selector: 'app-clients',
  imports: [
    RouterLink, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent, StatusBadgeComponent,
    CopyButtonComponent, ClientLogoComponent, LucideAppWindow, LucideChevronRight, LucideLock, LucidePlus
  ],
  template: `
    <app-page-header title="Clients" subtitle="Applications that sign users in or call APIs on their behalf.">
      @if (canEdit) {
        <div actions>
          <a class="btn btn-primary btn-sm" routerLink="add"><svg lucidePlus class="size-4"></svg>New client</a>
        </div>
      }
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" placeholder="Search clients" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th><app-sort-header [list]="list" field="clientName">Client</app-sort-header></th>
            <th><app-sort-header [list]="list" field="clientId">Client id</app-sort-header></th>
            <th><app-sort-header [list]="list" field="enabled">Status</app-sort-header></th>
            <th class="w-12"><span class="sr-only">Open</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="4"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (client of list.items(); track client.clientId) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td class="max-w-md">
                  <a class="flex items-center gap-3 hover:text-primary" [routerLink]="[client.clientId]">
                    <app-client-logo [src]="client.logoUri" [name]="client.clientName || client.clientId" />
                    <span class="min-w-0">
                      <span class="block truncate font-semibold">{{ client.clientName || client.clientId }}</span>
                      @if (client.description) {
                        <span class="block truncate text-xs text-base-content/60">{{ client.description }}</span>
                      }
                    </span>
                  </a>
                </td>
                <td>
                  <span class="inline-flex items-center gap-1">
                    <code class="code-chip">{{ client.clientId }}</code>
                    <app-copy-button [value]="client.clientId" label="client id" />
                    @if (client.nonEditable) {
                      <span class="tooltip" data-tip="Reserved client"><svg lucideLock class="size-3.5 text-warning" aria-label="Reserved"></svg></span>
                    }
                  </span>
                </td>
                <td>
                  @if (client.enabled) {
                    <app-status-badge tone="success">Enabled</app-status-badge>
                  } @else {
                    <app-status-badge tone="neutral">Disabled</app-status-badge>
                  }
                </td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="[client.clientId]" [attr.aria-label]="'Open ' + client.clientId">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4">
                  <app-empty-state [title]="list.failed() ? 'Clients could not be loaded' : list.hasFilters() ? 'No matching clients' : 'No clients yet'"
                    [message]="list.hasFilters() ? 'Try a different search term.' : 'Register an application to let it sign users in or call your APIs.'">
                    <svg icon lucideAppWindow class="size-5"></svg>
                    @if (canEdit && !list.hasFilters()) {
                      <a actions class="btn btn-sm" routerLink="add"><svg lucidePlus class="size-4"></svg>New client</a>
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
export class ClientsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  protected readonly list = new ListState<ClientInfo>({
    sort: 'clientId',
    dir: 'asc',
    load: query => this._api.getClients(query.page, query.pageSize, toApiSort(query), query.search || undefined)
  });

  public ngOnInit(): void {
    this.list.start();
  }
}
