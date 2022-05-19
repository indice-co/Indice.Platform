import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { Contact, ContactResultSet, CreateDistributionListContactRequest, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ComboboxComponent } from 'src/app/shared/components/combobox/combobox.component';

@Component({
    selector: 'app-distribution-list-contact-create',
    templateUrl: './distribution-list-contact-create.component.html'
})
export class DistributionListContactCreateComponent implements OnInit, AfterViewInit {
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    @ViewChild('contactsCombobox', { static: false }) public contactsCombobox!: ComboboxComponent;
    private _distributionListId: string = '';

    constructor(
        private _changeDetector: ChangeDetectorRef,
        private _api: MessagesApiClient,
        private _router: Router
    ) { }

    public submitInProgress = false;
    public contacts: Contact[] = [];
    public isLoading: boolean = false;

    public ngOnInit(): void {
        this._distributionListId = this._router.url.split('/')[2];
    }

    public onContactsSearch(searchTerm: string | undefined): void {
        this.isLoading = true;
        this._api
            .getContacts(undefined, 1, 10, 'email', searchTerm, true)
            .subscribe((contacts: ContactResultSet) => {
                this.contacts = contacts.items!;
                this.isLoading = false;
            });
    }

    public onContactSelected(contact: Contact): void { }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        console.log(this.contactsCombobox.selectedItems);
        var tasks = this.contactsCombobox.selectedItems.map((contact: Contact) => {
            const body = new CreateDistributionListContactRequest({
                email: contact.email,
                firstName: contact.firstName,
                fullName: contact.fullName,
                id: contact.id,
                lastName: contact.lastName,
                phoneNumber: contact.phoneNumber,
                recipientId: contact.recipientId,
                salutation: contact.salutation
            });
            return this._api.addContactToDistributionList(this._distributionListId, body);
        });
        forkJoin(tasks).subscribe().add(() => {
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'contacts']));
        });
    }
}
