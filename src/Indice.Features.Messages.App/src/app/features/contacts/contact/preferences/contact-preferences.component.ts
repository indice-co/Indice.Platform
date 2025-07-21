import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { RecepientPreference, ContactChannelKind, Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactStore } from '../contact-store.service';

@Component({
  selector: 'app-contact-preferences',
  templateUrl: './contact-preferences.component.html'
})
export class ContactPreferencesComponent implements OnInit {
  private _contactId: string | undefined;
  recepientPreference: RecepientPreference | undefined;
  protected ContactChannelKindEnum = ContactChannelKind;

  constructor(
    private readonly _ContactStore: ContactStore,
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _api: MessagesApiClient
  ) { }

  public _contact: Contact | undefined;

  public ngOnInit(): void {
    this._contactId = this._activatedRoute.parent?.snapshot.params['contactId'];
    if (this._contactId) {
      this._ContactStore.getContact(this._contactId).subscribe((item: Contact) => {
        this._contact = item;
        console.log(item);
      });
      this._api.getCommunicationPreferences(this._contactId).subscribe((communicationPreference: RecepientPreference) => {
        this.recepientPreference = communicationPreference;
      });
    }
  }

  public CheckReceivePreference(communicationPreferences: ContactChannelKind[], option: ContactChannelKind): boolean {
    return communicationPreferences.indexOf(option) >= 0 || communicationPreferences.indexOf(ContactChannelKind.Any) >= 0;
  }
  
}
