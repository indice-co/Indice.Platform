import { AfterViewInit, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';

import { forkJoin } from 'rxjs';
import { ContactPreferences, CreateContactRequest, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ListContactCreateComponent } from 'src/app/shared/components/list-contact-create/list-contact-create.component';

@Component({
  selector: 'app-contact-create',
  templateUrl: './contact-create.component.html'
})
export class ContactCreateComponent implements OnInit, AfterViewInit {
  @ViewChild('#contactCreateComponent', { static: false }) public contactCreateComponent!: ListContactCreateComponent;

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    private _router: Router
  ) { }

  public submitInProgress = false;

  public ngOnInit(): void {
  }

  public ngAfterViewInit(): void {
    this._changeDetector.detectChanges();
  }

  public onSubmit(contacts: ContactPreferences[]): void {
    if (!contacts) return;
    this.submitInProgress = true;
    var tasks = contacts.map((contact: ContactPreferences) => {
      const body = new CreateContactRequest({
        email: contact.email,
        firstName: contact.firstName,
        fullName: contact.fullName,
        lastName: contact.lastName,
        phoneNumber: contact.phoneNumber,
        recipientId: contact.recipientId,
        salutation: contact.salutation,
        communicationPreference: contact.preferences
      });
      return this._api.createContact(body);
    });
    forkJoin(tasks).subscribe().add(() => {
      this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['contacts']));
    });
  }
}
