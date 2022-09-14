import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TenantService } from '@indice/ng-auth';

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
        @Inject(ToasterService) private _toaster: ToasterService,
        private _tenantService: TenantService
    ) { }

    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public model = new UpdateContactRequest();

    public ngOnInit(): void {
        this._contactId = this._activatedRoute.snapshot.params['contactId'];
        this._distributionListId = this._router.url.split('/')[settings.multitenancy ? 3 : 2];
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
                    this._toaster.show(ToastType.Success, 'Επιτυχής αποθήκευση', `Η επαφή '${this.model.fullName || this.model.email}' ενημερώθηκε με επιτυχία.`);
                    const navigationCommands = ['distribution-lists', this._distributionListId, 'contacts'];
                    const tenantAlias = this._tenantService.getTenantValue();
                    if (tenantAlias !== '') {
                        navigationCommands.unshift(tenantAlias);
                    }
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(navigationCommands));
                }
            });
    }
}
