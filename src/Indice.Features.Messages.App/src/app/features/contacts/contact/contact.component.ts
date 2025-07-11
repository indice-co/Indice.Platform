import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { HeaderMetaItem, ViewLayoutComponent } from '@indice/ng-components';
import { Contact } from 'src/app/core/services/messages-api.service';
import { ContactStore } from './contact-store.service';

@Component({
    selector: 'app-contact',
    templateUrl: './contact.component.html'
})
export class ContactComponent implements OnInit, AfterViewChecked {
    @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
    private _contactId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
      private _ContactStore: ContactStore
    ) { }

    public submitInProgress = false;
    public Contact: Contact | undefined;
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
      this._contactId = this._activatedRoute.snapshot.params['contactId'];
      if (this._contactId) {
        this._ContactStore.getContact(this._contactId).subscribe((Contact: Contact) => {
              this.Contact = Contact;
              this._layout.title = `${Contact.fullName}`;
            });
        }
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }
}
