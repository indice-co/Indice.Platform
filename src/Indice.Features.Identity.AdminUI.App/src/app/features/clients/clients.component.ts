import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
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
    RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent, StatusBadgeComponent,
    CopyButtonComponent, ClientLogoComponent, LucideAppWindow, LucideChevronRight, LucideLock, LucidePlus
  ],
  template: `
    <app-page-header [title]="'Routes.Clients' | translate" [subtitle]="'Clients.List.Subtitle' | translate" [count]="list.total()">
      @if (canEdit) {
        <div actions>
          <a class="btn btn-primary btn-sm" routerLink="add"><svg lucidePlus class="size-4"></svg>{{ 'Routes.NewClient' | translate }}</a>
        </div>
      }
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Clients.List.SearchPlaceholder' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th app-sort-header [list]="list" field="clientName">{{ 'Clients.List.Client' | translate }}</th>
            <th app-sort-header [list]="list" field="clientId">{{ 'Clients.Fields.ClientId' | translate }}</th>
            <th app-sort-header [list]="list" field="enabled">{{ 'Common.Status' | translate }}</th>
            <th class="w-12"><span class="sr-only">{{ 'Clients.List.Open' | translate }}</span></th>
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
                    <app-copy-button [value]="client.clientId" [label]="'Clients.Fields.ClientIdLabel' | translate" />
                    @if (client.nonEditable) {
                      <span class="tooltip" [attr.data-tip]="'Clients.List.ReservedClient' | translate"><svg lucideLock class="size-3.5 text-warning" [attr.aria-label]="'Common.Reserved' | translate"></svg></span>
                    }
                  </span>
                </td>
                <td>
                  @if (client.enabled) {
                    <app-status-badge tone="success">{{ 'Common.Enabled' | translate }}</app-status-badge>
                  } @else {
                    <app-status-badge tone="neutral">{{ 'Common.Disabled' | translate }}</app-status-badge>
                  }
                </td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="[client.clientId]" [attr.aria-label]="'Clients.List.OpenClient' | translate: { id: client.clientId }">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4">
                  <app-empty-state [title]="(list.failed() ? 'Clients.List.LoadFailed' : list.hasFilters() ? 'Clients.List.NoMatches' : 'Clients.List.Empty') | translate"
                    [message]="(list.hasFilters() ? 'Clients.List.NoMatchesMessage' : 'Clients.List.EmptyMessage') | translate">
                    <svg icon lucideAppWindow class="size-5"></svg>
                    @if (canEdit && !list.hasFilters()) {
                      <a actions class="btn btn-sm" routerLink="add"><svg lucidePlus class="size-4"></svg>{{ 'Routes.NewClient' | translate }}</a>
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
