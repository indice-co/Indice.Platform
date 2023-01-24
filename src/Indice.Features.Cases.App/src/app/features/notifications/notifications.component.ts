import { Component, ViewChild } from "@angular/core";
import { NgForm } from "@angular/forms";
import { Router } from "@angular/router";
import { ToasterService, ToastType } from "@indice/ng-components";
import { EMPTY } from "rxjs";
import { tap, catchError } from "rxjs/operators";
import { CasesApiService, NotificationSubscriptionResult } from "src/app/core/services/cases-api.service";

@Component({
    selector: 'app-notifications',
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent {
    @ViewChild('notificationsForm', { static: false }) private notificationsForm!: NgForm;
    public notificationsAccepted: boolean = false;
    private _submittedNotificationValue: boolean | undefined;
    constructor(
        private api: CasesApiService,
        private router: Router,
        private toaster: ToasterService) {
        this.api.getMySubscriptions()
            .pipe(
                tap((response: NotificationSubscriptionResult) => {
                    this.notificationsAccepted = response.subscribed || false;
                    this._submittedNotificationValue = this.notificationsAccepted;
                }))
            .subscribe();
    }

    public canSubmit(): boolean {
        return this.notificationsForm?.valid === true;
    }

    public onSubmit(): void {
        if (!this.canSubmit()) {
            return;
        }
        const http$ = this.notificationsAccepted
            ? this.api.subscribe()
            : this.api.unsubscribe();

        http$.pipe(
            tap(_ => {
                const message = this.notificationsAccepted ? `Εγγραφήκατε στις ειδοποιήσεις επιτυχώς` : `Διαγραφήκατε από τις ειδοποιήσεις επιτυχώς`;
                this.toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', message);
                // set the form status to Pending so the form will be invalid, unless the user interacts with the UI
                this.notificationsForm.form.markAsPending();
                this._submittedNotificationValue = this.notificationsAccepted;
            }),
            catchError(() => {
                this.toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Η εγγραφή στις ειδοποιήσεις απέτυχε`, 6000);
                return EMPTY;
            })
        ).subscribe();
    }

    public notificationValueChanged(): boolean {
        return this._submittedNotificationValue !== this.notificationsAccepted
    }
}