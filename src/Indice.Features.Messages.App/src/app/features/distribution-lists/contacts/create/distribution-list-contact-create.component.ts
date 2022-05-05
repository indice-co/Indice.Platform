import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Contact, ContactResultSet, CreateDistributionListRequest, MessagesApiClient, MessageType, ValidationProblemDetails } from 'src/app/core/services/messages-api.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';

@Component({
    selector: 'app-distribution-list-contact-create',
    templateUrl: './distribution-list-contact-create.component.html'
})
export class DistributionListContactCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    private _distributionListId: string = '';

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _utilities: UtilitiesService,
        private _activatedRoute: ActivatedRoute
    ) { }

    public submitInProgress = false;
    public contacts: Contact[] = [];

    public ngOnInit(): void {
        this._distributionListId = this._router.url.split('/')[2];
        this._api
            .getContacts(undefined, 1, 10, 'email', undefined, true)
            .subscribe((contacts: ContactResultSet) => {
                this.contacts = contacts.items!;
            });
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        // this._api
        //     .createDistributionList(this.model)
        //     .subscribe({
        //         next: (messageType: MessageType) => {
        //             this.submitInProgress = false;
        //             this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η λίστα με όνομα '${messageType.name}' δημιουργήθηκε με επιτυχία.`);
        //             this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
        //         },
        //         error: (problemDetails: ValidationProblemDetails) => {
        //             this._toaster.show(ToastType.Error, 'Αποτυχία αποθήκευσης', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
        //         }
        //     });
    }
}
