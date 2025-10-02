import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ContactPreference, ContactChannelOption, ContactChannelKind, Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactStore } from '../contact-store.service';

@Component({
    selector: 'app-contact-preferences',
    templateUrl: './contact-preferences.component.html',
    standalone: false
})
export class ContactPreferencesComponent implements OnInit {
  private _contactId: string | undefined;
  recepientPreference: ContactPreference | undefined;
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
      this._api.getCommunicationPreferences(this._contactId).subscribe((communicationPreference: ContactPreference) => {
        this.recepientPreference = communicationPreference;
      });
    }
  }

  public CheckReceivePreference(communicationPreferences: ContactChannelOption[], option: ContactChannelKind): boolean {
    return communicationPreferences.findIndex(obj => obj.kind === option && obj.include) >= 0;
  }

}
