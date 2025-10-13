import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Contact } from 'src/app/core/services/messages-api.service';
import { ContactStore } from '../contact-store.service';

@Component({
  selector: 'app-contact-details',
  templateUrl: './contact-details.component.html'
})
export class ContactDetailsComponent implements OnInit {
  private _contactId: string | undefined;

  constructor(
    private readonly _ContactStore: ContactStore,
    private readonly _activatedRoute: ActivatedRoute
  ) { }

  public _contact: Contact | undefined;

  public ngOnInit(): void {
    this._contactId = this._activatedRoute.parent?.snapshot.params['contactId'];
    if (this._contactId) {
      this._ContactStore.getContact(this._contactId).subscribe((item: Contact) => {
        this._contact = item;
        console.log(item);
      });
    }
  }

  public reload(recipientId: string): void {
    this._ContactStore.reload(recipientId).subscribe();
  }
}
