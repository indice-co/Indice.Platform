import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, forkJoin } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { DisplayNotificationSubscriptionsViewModel, NotificationSubscriptionCategoryViewModel, NotificationSubscriptionViewModel } from 'src/app/core/models/NotificationSubscriptionsViewModel';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial, NotificationSubscription, NotificationSubscriptionRequest } from 'src/app/core/services/cases-api.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { PageHeaderComponent } from 'src/app/ui/page/page-header.component';

/** E-mail subscriptions to new cases, one checkbox per case type, grouped by case-type category. */
@Component({
  selector: 'app-notifications',
  imports: [FormsModule, NgIcon, TranslatePipe, PageHeaderComponent],
  template: `
    <div class="page max-w-6xl">
      <app-page-header [title]="'notifications.title' | translate" />

      <!-- One settings row: what the setting is and what it is set to on the left, the choices on the panel. -->
      <form class="grid gap-6 lg:grid-cols-[minmax(0,20rem)_minmax(0,1fr)] lg:gap-10" (ngSubmit)="onSubmit()">
        <div class="lg:pt-4">
          <h2 class="text-base font-medium">{{ 'notifications.subscribeHeading' | translate }}</h2>
          <p class="mt-1 text-quiet">{{ 'notifications.description' | translate }}</p>
          @if (model(); as viewModel) {
            <p class="mt-5 flex items-baseline gap-2">
              <span class="text-xl leading-7 kbd-ref">{{ subscribedCount(viewModel) }}</span>
              <span class="text-[0.8125rem] text-quiet">{{ 'notifications.summary' | translate: { total: totalCount(viewModel) } }}</span>
            </p>
          }
        </div>

        <div class="rounded-2xl bg-base-200 p-4 sm:p-5">
          @if (model(); as viewModel) {
            <div class="space-y-5">
              @for (category of viewModel.categories; track category.name) {
                <fieldset>
                  <legend class="mb-2 text-sm font-medium">{{ category.name }}</legend>
                  <div class="grid gap-2 sm:grid-cols-2">
                    @for (item of category.notificationSubscriptions; track item.notificationSubscription?.caseTypeId) {
                      <label class="flex cursor-pointer items-center gap-3 rounded-field border border-base-300 bg-base-100 px-3 py-2.5 transition-colors hover:border-[var(--cases-chart-axis)]"
                             [attr.for]="'id-' + item.notificationSubscription?.caseTypeId">
                        <input type="checkbox" class="checkbox checkbox-primary checkbox-sm"
                               [id]="'id-' + item.notificationSubscription?.caseTypeId"
                               [name]="'name-' + item.notificationSubscription?.caseTypeId"
                               [(ngModel)]="item.subscribed" />
                        <span>{{ item.title }}</span>
                      </label>
                    }
                  </div>
                </fieldset>
              }
            </div>
          } @else {
            <div class="space-y-3" aria-busy="true">
              @for (row of [1, 2, 3, 4]; track row) {
                <div class="skeleton h-11 w-full bg-base-100"></div>
              }
            </div>
          }

          <div class="mt-5 flex justify-end">
            <button type="submit" class="btn btn-primary" [disabled]="formSubmitting() || !model()">
              @if (formSubmitting()) {
                <span class="loading loading-spinner loading-sm"></span>
              } @else {
                <ng-icon name="lucideSave" size="1rem" />
              }
              {{ 'common.save' | translate }}
            </button>
          </div>
        </div>
      </form>
    </div>
  `
})
export class NotificationsComponent implements OnInit {
  private readonly api = inject(CasesApiService);
  private readonly toaster = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly caseTypeService = inject(CaseTypeService);

  protected readonly model = signal<DisplayNotificationSubscriptionsViewModel | undefined>(undefined);
  protected readonly formSubmitting = signal(false);
  private noCategoryName = 'ΛΟΙΠΕΣ';

  protected subscribedCount(viewModel: DisplayNotificationSubscriptionsViewModel): number {
    return (viewModel.categories ?? []).reduce((sum, category) => sum + (category.notificationSubscriptions ?? []).filter(item => item.subscribed).length, 0);
  }

  protected totalCount(viewModel: DisplayNotificationSubscriptionsViewModel): number {
    return (viewModel.categories ?? []).reduce((sum, category) => sum + (category.notificationSubscriptions ?? []).length, 0);
  }

  public ngOnInit(): void {
    forkJoin({
      getMySubscriptions: this.api.getMySubscriptions(),
      getCaseTypes: this.caseTypeService.getCaseTypes()
    }).subscribe(({ getMySubscriptions: mySubscriptions, getCaseTypes: caseTypes }) => {
      this.noCategoryName = this.translate.instant('notifications.otherCategory');
      // All categories and case types (server order) with no active subscription, then mark the active ones.
      const viewModel = this.createEmptyViewModel((caseTypes as { items?: CaseTypePartial[] }).items!);
      this.addActiveSubscriptions(viewModel, mySubscriptions.items!);
      this.model.set(viewModel);
    });
  }

  protected onSubmit(): void {
    this.formSubmitting.set(true);
    this.api.subscribe(this.createRequest()).pipe(
      tap(_ => {
        this.formSubmitting.set(false);
        this.toaster.show(ToastType.Success, 'toasts.saveSuccess.title', 'toasts.settingsSaved.body', 5000);
      }),
      catchError(() => {
        this.formSubmitting.set(false);
        this.toaster.show(ToastType.Error, 'toasts.saveError.title', 'toasts.settingsSaveError.body', 5000);
        return EMPTY;
      })
    ).subscribe();
  }

  private createRequest(): NotificationSubscriptionRequest {
    const caseTypeIds: string[] = [];
    this.model()?.categories?.forEach(c =>
      c.notificationSubscriptions?.filter(x => x.subscribed).forEach(n =>
        caseTypeIds.push(n.notificationSubscription!.caseTypeId!)));
    return new NotificationSubscriptionRequest({ caseTypeIds });
  }

  private addActiveSubscriptions(viewModel: DisplayNotificationSubscriptionsViewModel, subscriptions: NotificationSubscription[]): void {
    subscriptions?.forEach(active => {
      viewModel.categories?.forEach(category => {
        category.notificationSubscriptions?.forEach(n => {
          if (n.notificationSubscription?.caseTypeId === active.caseTypeId) {
            n.subscribed = true;
            n.notificationSubscription = active;
          }
        });
      });
    });
  }

  private createEmptyViewModel(caseTypes: CaseTypePartial[]): DisplayNotificationSubscriptionsViewModel {
    const categories = new Map<string, NotificationSubscriptionCategoryViewModel>();
    for (const caseType of caseTypes) {
      const name = caseType?.category?.name ?? this.noCategoryName;
      if (!categories.has(name)) {
        categories.set(name, { name, notificationSubscriptions: [] });
      }
      categories.get(name)!.notificationSubscriptions!.push(
        new NotificationSubscriptionViewModel(new NotificationSubscription({ caseTypeId: caseType.id }), false, caseType.title!));
    }
    return { categories: Array.from(categories.values()) };
  }
}
