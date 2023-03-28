import { CaseTypePartial } from './../../core/services/cases-api.service';
import { Component, OnInit } from "@angular/core";
import { AuthService } from "@indice/ng-auth";
import { ToasterService, ToastType } from "@indice/ng-components";
import { EMPTY, forkJoin } from "rxjs";
import { tap, catchError } from "rxjs/operators";
import { DisplayNotificationSubscriptionsViewModel, NotificationSubscriptionCategoryViewModel, NotificationSubscriptionViewModel } from "src/app/core/models/NotificationSubscriptionsViewModel";
import { CasesApiService, NotificationSubscription, NotificationSubscriptionRequest } from "src/app/core/services/cases-api.service";

@Component({
    selector: 'app-notifications',
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit {
    public displayNotificationSubscriptionsViewModel: DisplayNotificationSubscriptionsViewModel | undefined;
    public formSubmitting: boolean = false;
    public loading: boolean = false;
    public isAdmin: boolean = false;
    public noCategoryName: string = 'ΛΟΙΠΕΣ';

    constructor(
        private _api: CasesApiService,
        private authService: AuthService,
        private _toaster: ToasterService
    ) { }

    public ngOnInit(): void {
        // awful hack due to @indice/ng-auth's weird behavior
        this.isAdmin = this.authService.isAdmin();
        this.loading = true;
        forkJoin({
            getMySubscriptions: this._api.getMySubscriptions(),
            getCaseTypes: this._api.getCaseTypes()
        })
            .subscribe(({ getMySubscriptions: mySubscriptions, getCaseTypes: caseTypes }) => {
                // create an initial view model that contains all categories and case types (and respects server-side ordering) with no active subs
                this.displayNotificationSubscriptionsViewModel = this.createEmptyDisplayNotificationSubscriptionsViewModel(caseTypes.items!);
                // add active subscriptions
                this.addActiveSubscriptions(mySubscriptions.notificationSubscriptions!);
                this.loading = false;
            });
    }

    public onSubmit(): void {
        this.formSubmitting = true;
        const request = this.createNotificationSubscriptionRequest();
        this._api.subscribe(undefined, request).pipe(
            tap(_ => {
                this.formSubmitting = false;
                this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Οι ρυθμίσεις σας αποθηκεύτηκαν επιτυχώς.`, 5000);
            }),
            catchError(() => {
                this.formSubmitting = false;
                this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Δεν κατέστη εφικτή η αποθήκευση των ρυθμίσεών σας.`, 5000);
                return EMPTY;
            })
        ).subscribe();
    }

    private createNotificationSubscriptionRequest() {
        let caseTypeIds: string[] = [];
        this.displayNotificationSubscriptionsViewModel?.categories?.forEach(c =>
            c.notificationSubscriptions?.filter(x => x.subscribed).forEach(n =>
                caseTypeIds.push(n.notificationSubscription!.caseTypeId!)));
        return new NotificationSubscriptionRequest({ caseTypeIds: caseTypeIds })
    }

    private addActiveSubscriptions(notificationSubscriptions: NotificationSubscription[]) {
        notificationSubscriptions?.forEach(activeSubscription => {
            this.displayNotificationSubscriptionsViewModel?.categories?.forEach(category => {
                category.notificationSubscriptions?.forEach(n => {
                    if (n.notificationSubscription?.caseTypeId === activeSubscription.caseTypeId) {
                        n.subscribed = true;
                        n.notificationSubscription = activeSubscription;
                    }
                })
            })
        });
    }

    private createEmptyDisplayNotificationSubscriptionsViewModel(caseTypes: CaseTypePartial[]): DisplayNotificationSubscriptionsViewModel {
        const categoriesMap = new Map<string, NotificationSubscriptionCategoryViewModel>();
        for (const caseType of caseTypes) {
            this.addCaseTypeToCategory(categoriesMap, caseType);
        }
        return { categories: Array.from(categoriesMap.values()) };
    }

    private addCaseTypeToCategory(categoriesMap: Map<string, NotificationSubscriptionCategoryViewModel>, caseType: CaseTypePartial) {
        let categoryName = caseType?.category?.name ?? this.noCategoryName
        if (!categoriesMap.has(categoryName)) {
            categoriesMap.set(
                categoryName,
                { name: categoryName, notificationSubscriptions: [] }
            );
        }
        categoriesMap.get(categoryName)!.notificationSubscriptions!.push(new NotificationSubscriptionViewModel(new NotificationSubscription({ caseTypeId: caseType.id }), false, caseType.title!));
    }

}