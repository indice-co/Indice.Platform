import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { CommunicationPreference, CommunicationPreferences, Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactStore } from '../contact-store.service';

@Component({
  selector: 'app-contact-preferences',
  templateUrl: './contact-preferences.component.html'
})
export class ContactPreferencesComponent implements OnInit {
  private _contactId: string | undefined;
  communicationPreference: CommunicationPreference | undefined;
  protected communicationPreferencesEnum = CommunicationPreferences;

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
      this._api.getCommunicationPreferences(this._contactId).subscribe((communicationPreference: CommunicationPreference) => {
        this.communicationPreference = communicationPreference;
      });
    }
  }

  public hasEmail(communicationPreferences: CommunicationPreferences[], option: CommunicationPreferences): boolean {
    return communicationPreferences.indexOf(option) >= 0;  
  }
  public hasSMS(communicationPreferences?: CommunicationPreferences[]): boolean {
    if (communicationPreferences === undefined) return false;
    return communicationPreferences?.indexOf(CommunicationPreferences.SMS) >= 0;
  }
  public hasPush(communicationPreferences?: CommunicationPreferences[]): boolean {
    if (communicationPreferences === undefined) return false;
    return communicationPreferences?.indexOf(CommunicationPreferences.PushNotification) >= 0;
  }
}
