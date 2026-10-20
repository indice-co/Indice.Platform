import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CasesApiService, Query } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { ConfirmDialogComponent, ConfirmDialogData } from 'src/app/ui/feedback/confirm-dialog.component';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { SidePaneComponent } from 'src/app/ui/side-pane/side-pane.component';

/** "My searches": the user's saved case-list filters, to re-apply or delete. */
@Component({
  selector: 'app-queries-page',
  imports: [NgIcon, TranslatePipe, SidePaneComponent, EmptyStateComponent],
  template: `
    <app-side-pane [title]="'cases.mySearches' | translate" [subtitle]="'cases.mySearchesHint' | translate" [okShow]="false" [cancelShow]="false">
      @if (queries.isLoading() && !queries.hasValue()) {
        <div class="space-y-2">
          @for (row of [1, 2, 3]; track row) {
            <div class="skeleton h-12 w-full"></div>
          }
        </div>
      } @else if (queries.value()?.length) {
        <ul class="divide-y divide-base-300 overflow-hidden rounded-box border border-base-300">
          @for (query of queries.value(); track query.id) {
            <li class="flex items-center gap-2 py-2 pr-2 pl-4 transition-colors hover:bg-base-200/60">
              <ng-icon name="lucideBookmark" size="1rem" class="shrink-0 text-quiet" />
              <button type="button" class="min-w-0 flex-1 truncate py-1.5 text-left font-medium hover:text-primary" (click)="applyQuery(query)">
                {{ query.friendlyName }}
              </button>
              <button type="button" class="btn btn-ghost btn-sm" (click)="applyQuery(query)">{{ 'common.apply' | translate }}</button>
              <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="deleteQuery(query)"
                      [attr.aria-label]="('common.delete' | translate) + ' ' + query.friendlyName">
                <ng-icon name="lucideTrash2" size="1rem" />
              </button>
            </li>
          }
        </ul>
      } @else {
        <app-empty-state icon="lucideBookmark" [title]="'cases.noSavedSearches' | translate" [message]="'cases.noSavedSearchesHint' | translate" />
      }
    </app-side-pane>
  `
})
export class QueriesPageComponent {
  private readonly api = inject(CasesApiService);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly toaster = inject(ToastService);
  private readonly filterCaching = inject(FilterCachingService);

  protected readonly queries = rxResource({ stream: () => this.api.getQueries() });

  protected applyQuery(query: Query): void {
    this.filterCaching.resetParams('cases');
    // Leave and come back so the list re-reads its filters even when it is already open.
    this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => this.router.navigateByUrl(`/cases${query.parameters}`));
  }

  protected deleteQuery(query: Query): void {
    const data: ConfirmDialogData = {
      title: 'modals.deleteQuery.title',
      message: 'modals.deleteQuery.confirm',
      subject: query.friendlyName,
      confirmLabel: 'common.delete',
      tone: 'danger'
    };
    this.dialogs.open<ConfirmDialogComponent, boolean>(ConfirmDialogComponent, { data }).closed.subscribe(confirmed => {
      if (!confirmed) {
        return;
      }
      this.api.deleteQuery(query.id!).subscribe(() => {
        this.toaster.show(ToastType.Success, undefined, 'toasts.searchDeleted.body');
        this.queries.reload();
      });
    });
  }
}
