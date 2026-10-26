import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { LucideChevronRight, LucideFileUp, LucidePlus, LucideSlidersHorizontal } from '@lucide/angular';
import { AppSettingInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ListState, toApiSort } from 'src/app/ui/list/list-state';
import { PaginationComponent } from 'src/app/ui/list/pagination.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { SortHeaderComponent } from 'src/app/ui/list/sort-header.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

export const SETTINGS_REFRESH = 'settings';

/** Application settings stored in the database. Creating and editing happen in the side pane. */
@Component({
  selector: 'app-settings',
  imports: [
    RouterLink, PageHeaderComponent, EmptyStateComponent, PaginationComponent, SearchInputComponent, SortHeaderComponent, CopyButtonComponent,
    LucideChevronRight, LucideFileUp, LucidePlus, LucideSlidersHorizontal
  ],
  template: `
    <app-page-header title="App settings" subtitle="Configuration values the identity server reads from the database at runtime.">
      <div actions class="flex flex-wrap gap-2">
        <button type="button" class="btn btn-ghost btn-sm" [disabled]="importing()" (click)="import()">
          @if (importing()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideFileUp class="size-4"></svg> }
          Import appsettings.json
        </button>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('setting-add')"><svg lucidePlus class="size-4"></svg>New setting</a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" placeholder="Search settings" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th><app-sort-header [list]="list" field="key">Key</app-sort-header></th>
            <th>Value</th>
            <th class="w-12"><span class="sr-only">Open</span></th>
          </tr>
        </thead>
        <tbody>
          @if (list.loading() && !list.items()?.length) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr><td colspan="3"><div class="skeleton h-4 w-full"></div></td></tr>
            }
          } @else {
            @for (setting of list.items(); track setting.key) {
              <tr class="hover:bg-base-200/60" [class.opacity-60]="list.loading()">
                <td class="max-w-sm">
                  <a class="font-mono text-sm font-semibold break-all hover:text-primary" [routerLink]="pane.link(['setting', setting.key!])">{{ setting.key }}</a>
                </td>
                <td class="max-w-xl">
                  <span class="flex items-center gap-1">
                    <span class="line-clamp-2 font-mono text-xs break-all text-base-content/75">{{ setting.value || '—' }}</span>
                    @if (setting.value) { <app-copy-button [value]="setting.value" label="value" /> }
                  </span>
                </td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['setting', setting.key!])" [attr.aria-label]="'Open ' + setting.key">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="3">
                  <app-empty-state [title]="list.hasFilters() ? 'No matching settings' : 'No settings yet'"
                    [message]="list.hasFilters() ? 'Try a different search term.' : 'Add a setting, or import the ones in appsettings.json.'">
                    <svg icon lucideSlidersHorizontal class="size-5"></svg>
                    @if (!list.hasFilters()) {
                      <a actions class="btn btn-sm" [routerLink]="pane.link('setting-add')"><svg lucidePlus class="size-4"></svg>New setting</a>
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
export class SettingsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  protected readonly pane = inject(SidePaneService);
  protected readonly importing = signal(false);

  protected readonly list = new ListState<AppSettingInfo>({
    sort: 'key',
    dir: 'asc',
    load: query => this._api.getSettings(query.page, query.pageSize, toApiSort(query), query.search || undefined)
  });

  constructor() {
    inject(RefreshBus).on(SETTINGS_REFRESH).pipe(takeUntilDestroyed()).subscribe(() => this.list.refresh());
  }

  public ngOnInit(): void {
    this.list.start();
  }

  /** Loads appsettings.json into the database; once settings exist this needs a hard refresh, which replaces them. */
  protected import(): void {
    const hardRefresh = this.list.count() > 0 || this.list.hasFilters();
    this._dialog
      .confirm({
        title: 'Import appsettings.json?',
        message: hardRefresh
          ? 'The settings stored in the database will be replaced by the values in the server\'s appsettings.json file.'
          : 'The values in the server\'s appsettings.json file will be saved to the database.',
        confirmLabel: hardRefresh ? 'Replace settings' : 'Import',
        tone: hardRefresh ? 'danger' : 'default'
      })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.importing.set(true);
          return this._api.loadFromAppSettingsJson(hardRefresh);
        })
      )
      .subscribe({
        next: () => {
          this.importing.set(false);
          this._toast.success('Settings imported', 'appsettings.json was loaded into the database.');
          this.list.refresh();
        },
        error: () => this.importing.set(false)
      });
  }
}
