import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, catchError, finalize, forkJoin, take } from 'rxjs';
import { LucideBellOff, LucideSave } from '@lucide/angular';
import { NotificationSubscriptionCategoryViewModel } from 'src/app/core/models/NotificationSubscriptionsViewModel';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial, NotificationSubscription, NotificationSubscriptionRequest } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

/** Lets the user choose which case types they receive notifications for, grouped by case type category. */
@Component({
  selector: 'app-notifications',
  imports: [TranslatePipe, PageHeaderComponent, EmptyStateComponent, LucideBellOff, LucideSave],
  template: `
    <app-page-header [title]="'notifications.title' | translate" [subtitle]="'notifications.description' | translate" />

    @if (loading()) {
      <div class="panel max-w-3xl space-y-3 p-6">
        @for (row of [1, 2, 3, 4, 5]; track row) { <div class="skeleton h-6 w-full"></div> }
      </div>
    } @else {
      <form class="panel max-w-3xl" (submit)="$event.preventDefault(); save()">
        <div class="flex items-center justify-between gap-4 border-b border-base-300 px-5 py-4 sm:px-6">
          <h2 class="font-semibold">{{ 'notifications.subscribeHeading' | translate }}</h2>
          <span class="text-sm text-base-content/60">{{ 'notifications.selectedCount' | translate: { count: selectedCount() } }}</span>
        </div>
        @for (category of categories(); track category.name) {
          <section class="border-b border-base-300 px-5 py-4 last:border-b-0 sm:px-6">
            <div class="mb-2 flex items-center justify-between gap-4">
              <h3 class="text-sm font-semibold text-base-content/70">{{ category.name }}</h3>
              <button type="button" class="btn btn-ghost btn-xs" (click)="toggleCategory(category)">
                {{ (allSubscribed(category) ? 'notifications.clearAll' : 'notifications.selectAll') | translate }}
              </button>
            </div>
            <ul class="divide-y divide-base-200">
              @for (item of category.notificationSubscriptions; track item.caseTypeId) {
                <li>
                  <label class="flex cursor-pointer items-center justify-between gap-4 py-2.5">
                    <span class="text-sm">{{ item.title }}</span>
                    <input type="checkbox" class="toggle toggle-primary toggle-sm" [checked]="item.subscribed" (change)="setSubscribed(item.caseTypeId, $any($event.target).checked)" />
                  </label>
                </li>
              }
            </ul>
          </section>
        } @empty {
          <app-empty-state [title]="'notifications.empty.title' | translate">
            <svg icon lucideBellOff class="size-5"></svg>
          </app-empty-state>
        }
        <div class="sticky bottom-0 flex justify-end rounded-b-box border-t border-base-300 bg-base-100 px-5 py-3 sm:px-6">
          <button type="submit" class="btn btn-primary" [disabled]="saving() || !categories().length">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
            {{ 'common.save' | translate }}
          </button>
        </div>
      </form>
    }
  `
})
export class NotificationsComponent implements OnInit {
  private readonly _api = inject(CasesApiService);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _toaster = inject(TranslatedToasterService);
  private readonly _translate = inject(TranslateService);

  protected readonly categories = signal<NotificationSubscriptionCategoryViewModel[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly selectedCount = computed(() =>
    this.categories().reduce((sum, c) => sum + c.notificationSubscriptions.filter(n => n.subscribed).length, 0));

  public ngOnInit(): void {
    forkJoin({
      subscriptions: this._api.getMySubscriptions(),
      caseTypes: this._caseTypeService.getCaseTypes().pipe(take(1))
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ subscriptions, caseTypes }) => {
        this.categories.set(this.buildCategories(caseTypes.items ?? [], subscriptions.items ?? []));
      });
  }

  protected allSubscribed(category: NotificationSubscriptionCategoryViewModel): boolean {
    return category.notificationSubscriptions.every(n => n.subscribed);
  }

  protected toggleCategory(category: NotificationSubscriptionCategoryViewModel): void {
    const subscribed = !this.allSubscribed(category);
    this.categories.update(categories => categories.map(c => c !== category ? c : {
      ...c,
      notificationSubscriptions: c.notificationSubscriptions.map(n => ({ ...n, subscribed }))
    }));
  }

  protected setSubscribed(caseTypeId: string, subscribed: boolean): void {
    this.categories.update(categories => categories.map(c => ({
      ...c,
      notificationSubscriptions: c.notificationSubscriptions.map(n => n.caseTypeId === caseTypeId ? { ...n, subscribed } : n)
    })));
  }

  protected save(): void {
    const caseTypeIds = this.categories().flatMap(c => c.notificationSubscriptions.filter(n => n.subscribed).map(n => n.caseTypeId));
    this.saving.set(true);
    this._api.subscribe(new NotificationSubscriptionRequest({ caseTypeIds })).pipe(
      catchError(() => EMPTY),
      finalize(() => this.saving.set(false))
    ).subscribe(() => this._toaster.show(ToastType.Success, 'toasts.saveSuccess.title', 'toasts.settingsSaved.body', 5000));
  }

  /** Keeps server-side case type ordering; case types without a category go under "other". */
  private buildCategories(caseTypes: CaseTypePartial[], active: NotificationSubscription[]): NotificationSubscriptionCategoryViewModel[] {
    const otherCategory = this._translate.instant('notifications.otherCategory');
    const activeIds = new Set(active.map(s => s.caseTypeId));
    const categories = new Map<string, NotificationSubscriptionCategoryViewModel>();
    for (const caseType of caseTypes) {
      const name = caseType.category?.name ?? otherCategory;
      if (!categories.has(name)) {
        categories.set(name, { name, notificationSubscriptions: [] });
      }
      categories.get(name)!.notificationSubscriptions.push({
        caseTypeId: caseType.id!,
        title: caseType.title!,
        subscribed: activeIds.has(caseType.id)
      });
    }
    return Array.from(categories.values());
  }
}
