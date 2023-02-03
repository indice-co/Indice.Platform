import { Component, OnInit } from "@angular/core";
import { ToasterService, ToastType } from "@indice/ng-components";
import { EMPTY, forkJoin } from "rxjs";
import { tap, catchError } from "rxjs/operators";
import { NotificationSubscriptionViewModel } from "src/app/core/models/NotificationSubscriptionsViewModel";
import { CasesApiService, NotificationSubscription, NotificationSubscriptionRequest } from "src/app/core/services/cases-api.service";

@Component({
    selector: 'app-notifications',
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit {
    public notificationSubscriptionViewModels: NotificationSubscriptionViewModel[] = [];
    public formSubmitting: boolean = false;
    public loading: boolean = false;

    constructor(private _api: CasesApiService,
        private _toaster: ToasterService) { }

    public ngOnInit(): void {
        this.loading = true;
        forkJoin({
            getMySubscriptions: this._api.getMySubscriptions(),
            getCaseTypes: this._api.getCaseTypes()
        })
            .subscribe(({ getMySubscriptions, getCaseTypes }) => {
                // add active subscriptions
                getMySubscriptions.notificationSubscriptions?.forEach(sub => {
                    this.notificationSubscriptionViewModels?.push(new NotificationSubscriptionViewModel(sub, true));
                });
                // add inactive subscriptions
                getCaseTypes.items?.forEach(caseType => {
                    let subscription = this.notificationSubscriptionViewModels?.find(x => x.notificationSubscription?.caseTypeId === caseType.id)
                    if (!subscription) {
                        subscription = new NotificationSubscriptionViewModel(new NotificationSubscription({ caseTypeId: caseType.id }), false);
                        this.notificationSubscriptionViewModels?.push(subscription);
                    }
                    subscription.title = caseType.title;
                });
                this.loading = false;
            });
    }

    public onSubmit(): void {
        this.formSubmitting = true;
        let caseTypeIds: string[] = [];
        this.notificationSubscriptionViewModels?.forEach(x => {
            if (x.subscribed) {
                caseTypeIds.push(x.notificationSubscription?.caseTypeId!);
            }
        });
        this._api.subscribe(undefined, new NotificationSubscriptionRequest({ caseTypeIds: caseTypeIds })).pipe(
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

}