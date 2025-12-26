import { Injectable } from '@angular/core';

import { AsyncSubject, Observable, map, merge, identity, scan, ReplaySubject, Subject, switchMap } from 'rxjs';
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
    return this._api.resolveContact(recipientId);
  }






  private reloadSubj = new Subject<void>();
  private idRplSubj = new ReplaySubject<string>(1);

  public contact$: Observable<Contact> =
    combineReload(
      this.idRplSubj,
      this.reloadSubj
    )
    .pipe(
      switchMap((contactId: string) => {
        return this._api.getContactById(contactId);
      })
    );
}

function combineReload<T>(
  value$: Observable<T>,
  reload$: Observable<void>,
  selector: Function = identity
): Observable<T> {
  return merge(value$, reload$).pipe(
    reload(selector),
    map((value: any) => value as T)
  );
}
function reload(selector: Function = identity) {
  return scan((oldValue, currentValue) => {
    if (!oldValue && !currentValue)
      throw new Error(`Reload can't run before initial load`);

    return selector(currentValue || oldValue);
  });
}
