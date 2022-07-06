import { Component, ElementRef, Inject, ViewChild } from "@angular/core";
import { NgForm } from "@angular/forms";
import { Router } from "@angular/router";
import { ToasterService, ToastType } from "@indice/ng-components";
import { tap } from "rxjs/operators";
import { CasesApiService } from "src/app/core/services/cases-api.service";

@Component({
    selector: 'app-notifications',
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent {

    @ViewChild('notificationsForm', { static: false }) private notificationsForm!: NgForm;
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public notificationsAccepted: boolean = false;
    constructor(
        private _api: CasesApiService,
        private router: Router,
        @Inject(ToasterService) private toaster: ToasterService) {
    }

    public canSubmit(): boolean {
        return this.notificationsForm?.valid === true;
    }
    public onSubmit(): void {
        if (!this.canSubmit()) {
            return;
        }
        this.submitInProgress = true;
        this._api.createCaseTypeNotificationSubscription().pipe(
            tap(_ => {
                this.submitInProgress = false;
                this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => this.router.navigate(['notifications']));
                this.toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Εγγραφήκατε στις ειδοποιήσεις επιτυχώς`);
            }, error => {
                this.toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `Η εγγραφή στις ειδοποιήσεις απέτυχε`, 6000);
            })).subscribe();
    }
}