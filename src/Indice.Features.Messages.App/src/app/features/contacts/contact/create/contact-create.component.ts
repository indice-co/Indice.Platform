import { Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { Contact, CreateContactRequest, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ListContactCreateComponent } from 'src/app/shared/components/list-contact-create/list-contact-create.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Side pane that creates contacts picked or drafted with the contact picker. */
@Component({
  selector: 'app-contact-create',
  imports: [TranslatePipe, SidePaneLayoutComponent, ListContactCreateComponent],
  template: `
    <app-side-pane-layout [title]="'Contacts.AddContacts' | translate" [subtitle]="'Contacts.AddContactsSubtitle' | translate">
      <app-list-contact-create #picker (onSubmit)="create($event)" />
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Contacts.CancelLabel' | translate }}</button>
        <button type="button" class="btn btn-primary" [disabled]="saving() || !picker.selected().length" (click)="picker.submit()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Contacts.Add' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class ContactCreateComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);
  protected readonly saving = signal(false);

  protected create(contacts: Contact[]): void {
    if (!contacts.length) {
      return;
    }
    this.saving.set(true);
    const requests = contacts.map(contact => this._api.createContact(new CreateContactRequest({
      email: contact.email,
      firstName: contact.firstName,
      fullName: contact.fullName,
      lastName: contact.lastName,
      phoneNumber: contact.phoneNumber,
      recipientId: contact.recipientId,
      salutation: contact.salutation,
      resolved: contact.resolved || false,
      communicationPreference: contact.preference
    })));
    forkJoin(requests).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Contacts.CreateSuccessTitle', 'Contacts.CreateSuccessMessage', undefined, { count: contacts.length });
        this._refresh.notify('contacts');
        this.pane.close();
      },
      error: () => {
        this.saving.set(false);
        this._refresh.notify('contacts');
      }
    });
  }
}
