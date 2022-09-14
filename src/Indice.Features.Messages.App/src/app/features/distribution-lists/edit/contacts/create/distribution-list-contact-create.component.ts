import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { ComboboxComponent } from '@indice/ng-components';
import { settings } from 'src/app/core/models/settings';
import { forkJoin } from 'rxjs';
import { Contact, ContactResultSet, CreateDistributionListContactRequest, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { TenantService } from '@indice/ng-auth';

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
        private _router: Router,
        private _tenantService: TenantService
    ) { }

    public submitInProgress = false;
    public contacts: Contact[] = [];
    public isLoading: boolean = false;
    public apiUrl = settings.api_url;
    public get anyContactEditing() {
        return false;
    }

    public ngOnInit(): void {
        this._distributionListId = this._router.url.split('/')[settings.multitenancy ? 3 : 2];
    }

    public onContactsSearch(searchTerm: string | undefined): void {
        this.isLoading = true;
        this._api
            .getContacts(undefined, undefined, undefined, undefined, 1, 10, 'email', searchTerm, true)
            .subscribe((contacts: ContactResultSet) => {
                this.contacts = contacts.items || [];
                this.contacts.forEach((contact: Contact, index: number) => {
                    (<any>contact)['_index'] = index;
                });
                this.isLoading = false;
            });
    }

    public onContactSelected(contact: Contact): void { }

    public ngAfterViewInit(): void {
        this._changeDetector.detectChanges();
    }

    public onContactSaveChanges(item: any): void {
        delete item._edit;
    }

    public onAddNewContact(searchTerm: string): void {
        const validateEmail = (email: string) => {
            return String(email)
                .toLowerCase()
                .match(/^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/);
        };
        const contact = new Contact();
        searchTerm = searchTerm.trim()
        if (validateEmail(searchTerm)) {
            contact.email = searchTerm;
        } else {
            contact.fullName = searchTerm;
            contact.firstName = searchTerm.split(' ')[0];
            contact.lastName = searchTerm.slice(contact.firstName.length).trim();
        }
        (<any>contact)._edit = true;
        this.contactsCombobox.selectedItems.unshift(contact);
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        console.log(this.contactsCombobox.selectedItems);
        var tasks = this.contactsCombobox.selectedItems.map((contact: Contact) => {
            const body = new CreateDistributionListContactRequest({
                email: contact.email,
                firstName: contact.firstName,
                fullName: contact.fullName,
                contactId: contact.id,
                lastName: contact.lastName,
                phoneNumber: contact.phoneNumber,
                recipientId: contact.recipientId,
                salutation: contact.salutation
            });
            return this._api.addContactToDistributionList(this._distributionListId, body);
        });
        forkJoin(tasks).subscribe().add(() => {
            const navigationCommands = ['distribution-lists', this._distributionListId, 'contacts'];
            const tenantAlias = this._tenantService.getTenantValue();
            if (tenantAlias !== '') {
                navigationCommands.unshift(tenantAlias);
            }
            this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(navigationCommands));
        });
    }

    public onContactChangesSubmit(): void { }
}
