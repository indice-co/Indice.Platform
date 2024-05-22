import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Subscription } from 'rxjs';
import { Contact, MessagesApiClient, UpdateContactRequest } from 'src/app/core/services/messages-api.service';
import { settings } from 'src/app/core/models/settings';

@Component({
    selector: 'app-distribution-list-contact-edit',
    templateUrl: './distribution-list-contact-edit.component.html'
})
export class DistributionListContactEditComponent implements OnInit, AfterViewInit, OnDestroy {
    private _getContactSubscription!: Subscription;
    private _updateContactSubscription!: Subscription;
    private _contactId: string = '';
    private _distributionListId: string = '';

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public model = new UpdateContactRequest();

    public ngOnInit(): void {
        this._contactId = this._activatedRoute.snapshot.params['contactId'];
        this._distributionListId = this._router.url.split('/')[2];
        this._getContactSubscription = this
            ._api
            .getContactById(this._contactId)
            .subscribe((contact: Contact) => this.model = contact);
    }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public ngOnDestroy(): void {
        this._getContactSubscription?.unsubscribe();
        this._updateContactSubscription?.unsubscribe();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._updateContactSubscription = this._api
            .updateContact(this._contactId, this.model)
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'contacts-edit.save', `'contacts-edit.save-message' '${this.model.fullName || this.model.email}'`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'distribution-list-contacts']));
                }
            });
    }
}
