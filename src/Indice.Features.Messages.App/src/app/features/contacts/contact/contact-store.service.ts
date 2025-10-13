import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Injectable({
  providedIn: 'root'
})
export class ContactStore {
  private _contact: AsyncSubject<Contact> | undefined;
  private _idChanged = false;
  private _currentId = '';


  constructor(
    private readonly _api: MessagesApiClient
  ) { }

  public getContact(contactId: string): Observable<Contact> {

    this._idChanged = this._currentId !== contactId;
    this._currentId = contactId;
    if (!this._contact || this._idChanged) {
      this._contact = new AsyncSubject<Contact>();
      this._api
        .getContactById(contactId)
        .subscribe((Contact: Contact) => {
          this._contact?.next(Contact);
          this._contact?.complete();
        });
    }
    return this._contact;
  }

  public reload(recipientId: string) {
    return this._api.refreshContact(recipientId);
  }
}
