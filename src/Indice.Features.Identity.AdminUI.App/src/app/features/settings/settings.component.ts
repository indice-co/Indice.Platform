import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { LucideChevronRight, LucideFileUp, LucidePlus, LucideSlidersHorizontal } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
    LucideChevronRight, LucideFileUp, LucidePlus, LucideSlidersHorizontal, TranslatePipe
  ],
  template: `
    <app-page-header [title]="'Routes.AppSettings' | translate" [subtitle]="'Settings.List.Subtitle' | translate">
      <div actions class="flex flex-wrap gap-2">
        <button type="button" class="btn btn-ghost btn-sm" [disabled]="importing()" (click)="import()">
          @if (importing()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideFileUp class="size-4"></svg> }
          {{ 'Settings.List.Import' | translate }}
        </button>
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link('setting-add')"><svg lucidePlus class="size-4"></svg>{{ 'Settings.New' | translate }}</a>
      </div>
    </app-page-header>

    <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center">
      <app-search-input class="sm:w-72" [value]="list.search()" [placeholder]="'Settings.List.SearchPlaceholder' | translate" (search)="list.setSearch($event)" />
    </div>

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th app-sort-header [list]="list" field="key">{{ 'Settings.Fields.Key' | translate }}</th>
            <th>{{ 'Common.Value' | translate }}</th>
            <th class="w-12"><span class="sr-only">{{ 'Settings.List.Open' | translate }}</span></th>
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
                    @if (setting.value) { <app-copy-button [value]="setting.value" [label]="'Settings.List.CopyValueLabel' | translate" /> }
                  </span>
                </td>
                <td class="text-right">
                  <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['setting', setting.key!])" [attr.aria-label]="'Settings.List.OpenItem' | translate: { name: setting.key }">
                    <svg lucideChevronRight class="size-4"></svg>
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="3">
                  <app-empty-state [title]="(list.hasFilters() ? 'Settings.List.NoMatches' : 'Settings.List.Empty') | translate"
                    [message]="(list.hasFilters() ? 'Settings.List.NoMatchesHint' : 'Settings.List.EmptyHint') | translate">
                    <svg icon lucideSlidersHorizontal class="size-5"></svg>
                    @if (!list.hasFilters()) {
                      <a actions class="btn btn-sm" [routerLink]="pane.link('setting-add')"><svg lucidePlus class="size-4"></svg>{{ 'Settings.New' | translate }}</a>
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
  private readonly _translate = inject(TranslateService);
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
        title: this._translate.instant('Settings.Import.Title'),
        message: this._translate.instant(hardRefresh ? 'Settings.Import.ReplaceMessage' : 'Settings.Import.SaveMessage'),
        confirmLabel: this._translate.instant(hardRefresh ? 'Settings.Import.ReplaceConfirm' : 'Settings.Import.Confirm'),
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
          this._toast.success(this._translate.instant('Settings.Import.SuccessTitle'), this._translate.instant('Settings.Import.SuccessMessage'));
          this.list.refresh();
        },
        error: () => this.importing.set(false)
      });
  }
}
