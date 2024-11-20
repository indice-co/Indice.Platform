import { AfterViewInit, ChangeDetectorRef, Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { ComboboxComponent } from '@indice/ng-components';
import { settings } from 'src/app/core/models/settings';
import { MessagesApiClient, Contact, ContactResultSet } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-list-contact-create',
  templateUrl: './list-contact-create.component.html'
})
export class ListContactCreateComponent implements AfterViewInit {

  @ViewChild('contactsCombobox', { static: false }) public contactsCombobox!: ComboboxComponent;

  @Output() onSubmit: EventEmitter<Contact[]> = new EventEmitter<Contact[]>();
  @Output() onCancel: EventEmitter<Contact[]> = new EventEmitter<Contact[]>();

  constructor(
      private _changeDetector: ChangeDetectorRef,
      private _api: MessagesApiClient
  ) { }

  public submitInProgress = false;
  public contacts: Contact[] = [];
  public isLoading: boolean = false;
  public apiUrl = settings.api_url;
  public get anyContactEditing() {
      return false;
  }
  public savedContacts: Contact[] = [];

public onContactsSearch(searchTerm: string | undefined): void {
    this.isLoading = true;
    this._api
      .getContacts(1, 10, 'email', searchTerm, undefined, undefined, undefined, undefined, true)
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
    if (this.contactsCombobox.selectedItems.some(x => x.fullName === searchTerm)) {
        return;
    }
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

public submit(): void {
    this.savedContacts = JSON.parse(JSON.stringify(this.contactsCombobox.selectedItems));
    this.onSubmit.emit(this.contactsCombobox.selectedItems);
}

public cancel(): void {
    this.contactsCombobox.selectedItems = JSON.parse(JSON.stringify(this.savedContacts));
    this.onCancel.emit(this.contactsCombobox.selectedItems);
}

public reset(): void {
    this.contactsCombobox.selectedItems = [];
    this.savedContacts = [];
}

public onContactChangesSubmit(): void { }
}
