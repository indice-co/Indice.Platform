import { Component, OnInit } from "@angular/core";
import { ToasterService, ToastType } from "@indice/ng-components";
import { EMPTY } from "rxjs";
import { tap, catchError } from "rxjs/operators";
import { CasesApiService, NotificationSubscriptionDTO, NotificationSubscriptionSetting } from "src/app/core/services/cases-api.service";

@Component({
    selector: 'app-notifications',
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit {
    public notificationSubscriptionSettings: NotificationSubscriptionSetting[] | undefined;
    public formSubmitting: boolean = false;
    public loading: boolean = false;

    constructor(private _api: CasesApiService,
        private _toaster: ToasterService) { }

    public ngOnInit(): void {
        this.loading = true;
        this._api.getMySubscriptions()
            .pipe(
                tap((response: NotificationSubscriptionDTO) => {
                    this.notificationSubscriptionSettings = response.notificationSubscriptionSettings;
                    this.loading = false;
                }))
            .subscribe();
    }

    public onSubmit(): void {
        this.formSubmitting = true;
        this._api.subscribe(undefined, new NotificationSubscriptionDTO({ notificationSubscriptionSettings: this.notificationSubscriptionSettings })).pipe(
            tap(_ => {
                this.formSubmitting = false;
                const message = `Οι ρυθμίσεις σας αποθηκεύτηκαν επιτυχώς`;
                this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', message);
            }),
            catchError(() => {
                this.formSubmitting = false;
                this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Η εγγραφή στις ειδοποιήσεις απέτυχε`, 6000);
                return EMPTY;
            })
        ).subscribe();
    }

}