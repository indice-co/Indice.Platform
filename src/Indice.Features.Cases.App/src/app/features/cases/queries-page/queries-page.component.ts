import { Component, inject, signal } from '@angular/core';
import { Params, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, switchMap } from 'rxjs';
import { LucideBookmark, LucideTrash } from '@lucide/angular';
import { CasesApiService, Query } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { SIDE_PANE_OUTLET, SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** Side pane listing the user's saved cases searches. */
@Component({
  selector: 'app-queries-page',
  imports: [TranslatePipe, SidePaneLayoutComponent, EmptyStateComponent, LucideBookmark, LucideTrash],
  template: `
    <app-side-pane-layout [title]="'cases.mySearches' | translate" [subtitle]="'cases.savedSearches.subtitle' | translate">
      @if (queries(); as queries) {
        @if (queries.length) {
          <ul class="divide-y divide-base-300 overflow-hidden rounded-box border border-base-300">
            @for (query of queries; track query.id) {
              <li class="flex items-center gap-2 pr-2 hover:bg-base-200">
                <button type="button" class="flex min-w-0 flex-1 items-center gap-3 px-4 py-3 text-left focus-visible:outline-none focus-visible:underline"
                  (click)="apply(query)" [title]="'common.apply' | translate">
                  <svg lucideBookmark class="size-4 shrink-0 text-primary"></svg>
                  <span class="truncate font-medium">{{ query.friendlyName }}</span>
                </button>
                <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="remove(query)"
                  [attr.aria-label]="('common.delete' | translate) + ' ' + query.friendlyName">
                  <svg lucideTrash class="size-4"></svg>
                </button>
              </li>
            }
          </ul>
        } @else {
          <app-empty-state [title]="'cases.noSavedSearches' | translate" [message]="'cases.savedSearches.emptyHint' | translate">
            <svg icon lucideBookmark class="size-5"></svg>
          </app-empty-state>
        }
      } @else {
        <div class="space-y-2">
          @for (i of [1, 2, 3]; track i) { <div class="skeleton h-11 w-full"></div> }
        </div>
      }
    </app-side-pane-layout>
  `
})
export class QueriesPageComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _filterCaching = inject(FilterCachingService);

  protected readonly queries = signal<Query[] | undefined>(undefined);

  constructor() {
    this.load();
  }

  protected apply(query: Query): void {
    this._filterCaching.resetParams('cases');
    const queryParams: Params = {};
    const parsed = this._router.parseUrl(`/cases${query.parameters ?? ''}`).queryParams;
    // older saved searches may carry literal "null" / "undefined" values for unset params
    for (const [key, value] of Object.entries(parsed)) {
      if (value !== 'null' && value !== 'undefined' && value !== '') {
        queryParams[key] = value;
      }
    }
    this._router.navigate(['/', { outlets: { primary: ['cases'], [SIDE_PANE_OUTLET]: null } }], { queryParams });
  }

  protected remove(query: Query): void {
    this._dialog.confirm({
      title: 'modals.deleteQuery.title',
      message: 'modals.deleteQuery.confirm',
      confirmLabel: 'common.delete',
      tone: 'danger'
    })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteQuery(query.id!)))
      .subscribe(() => {
        this._toast.success('toasts.success.title', 'toasts.searchDeleted.body');
        this.load();
      });
  }

  private load(): void {
    this._api.getQueries().subscribe({
      next: queries => this.queries.set(queries ?? []),
      error: () => this.queries.set([])
    });
  }
}
