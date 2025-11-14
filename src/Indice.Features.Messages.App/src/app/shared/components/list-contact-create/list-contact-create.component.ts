import { AfterViewInit, ChangeDetectorRef, Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { EnhancedComboboxComponent } from '@indice/ng-components';
import { lastValueFrom } from 'rxjs';
import { settings } from 'src/app/core/models/settings';
import { MessagesApiClient, ContactResultSet, Contact } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-list-contact-create',
    templateUrl: './list-contact-create.component.html',
    standalone: false
})
export class ListContactCreateComponent implements AfterViewInit {

  @ViewChild('contactsCombobox', { static: false }) public contactsCombobox!: EnhancedComboboxComponent;

  @Output() onSubmit: EventEmitter<Contact[]> = new EventEmitter<Contact[]>();
  @Output() onCancel: EventEmitter<Contact[]> = new EventEmitter<Contact[]>();

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient
  ) { }

  public submitInProgress = false;
  public contacts: Contact[] = [];
  public isLoading: boolean = false;
  public avatarOrigin = (settings.api_url || "").substring(0, 4) === "http" ? new URL(settings.api_url).origin : "";
  public get anyContactEditing() {
    return false;
  }
  public savedContacts: Contact[] = [];

  public displayShowMoreOption: boolean = false;
  private _page: number = 1;
  private _pageSize: number = 6;
  private _lastSearchTerm: string | undefined = undefined;

  public async onContactsSearch(searchTerm: string | undefined): Promise<void> {
    this._page = 1;
    this._lastSearchTerm = searchTerm;
    this.isLoading = true;

    try {
      const fetchedContacts = await this._fetchContacts(this._lastSearchTerm);
      if (!!fetchedContacts.items) {
        this.contacts = fetchedContacts.items;
        this.contacts.forEach((contact: Contact, index: number) => {
          (<any>contact)['_index'] = index;
        });
        this.displayShowMoreOption = fetchedContacts.items.length === this._pageSize;
      }
    } catch (error) {
      console.error('Error fetching contacts:', error);
    } finally {
      this.isLoading = false;
    }
  }

  public async onShowMore(): Promise<void> {
    this._page++;
    this.isLoading = true;

    try {
      const fetchedContacts = await this._fetchContacts(this._lastSearchTerm);
      if (!!fetchedContacts.items) {
        this.contacts = [...this.contacts, ...fetchedContacts.items];
        this.contacts.forEach((contact: Contact, index: number) => {
          (<any>contact)['_index'] = index;
        });
        this.displayShowMoreOption = fetchedContacts.items.length === this._pageSize;
      }
    } catch (error) {
      console.error('Error fetching more contacts:', error);
    } finally {
      this.isLoading = false;
    }
  }

  private _fetchContacts(searchTerm: string | undefined): Promise<ContactResultSet> {
    return lastValueFrom(
      this._api.getContacts(this._page, this._pageSize, 'email', searchTerm, undefined, undefined, undefined, undefined, undefined, true)
    );
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
    searchTerm = searchTerm.trim();
    if (validateEmail(searchTerm)) {
      contact.email = searchTerm;
    } else {
      contact.fullName = searchTerm;
      contact.firstName = searchTerm.split(' ')[0];
      contact.lastName = searchTerm.slice(contact.firstName.length).trim();
    }
    (<any>contact)._edit = true;
    this.contactsCombobox.selectedItems.unshift(contact);
    this.contactsCombobox.busy = true;
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
