import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Contact, MessagesApiClient, UpdateContactRequest } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { BoolIconComponent } from 'src/app/ui/badges/badges';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/**
 * Side pane form that edits the contact in the `contactId` route param. Used both from the contact detail page
 * (`update-contact/:contactId`) and from a distribution list's contacts (`edit-contact/:contactId`).
 */
@Component({
  selector: 'app-contact-edit',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent, BoolIconComponent],
  template: `
    <app-side-pane-layout [title]="'Contacts.EditContact' | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3, 4, 5]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="contact-edit-form" class="space-y-1" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="contact-salutation">{{ 'Contacts.Salutation' | translate }}</label>
            <input id="contact-salutation" class="input w-full" formControlName="salutation" [placeholder]="'Contacts.SalutationPlaceholder' | translate" />
            <p class="label whitespace-normal">{{ 'Contacts.SalutationInfo' | translate }}</p>
          </fieldset>
          <div class="grid grid-cols-1 gap-x-4 sm:grid-cols-2">
            <fieldset class="fieldset">
              <label class="fieldset-legend" for="contact-first-name">{{ 'Contacts.FirstName' | translate }}</label>
              <input id="contact-first-name" class="input w-full" formControlName="firstName" [placeholder]="'Contacts.FirstNamePlaceholder' | translate" />
            </fieldset>
            <fieldset class="fieldset">
              <label class="fieldset-legend" for="contact-last-name">{{ 'Contacts.LastName' | translate }}</label>
              <input id="contact-last-name" class="input w-full" formControlName="lastName" [placeholder]="'Contacts.LastNamePlaceholder' | translate" />
            </fieldset>
          </div>
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="contact-full-name">{{ 'Contacts.FullName' | translate }}</label>
            <input id="contact-full-name" class="input w-full" formControlName="fullName" [placeholder]="'Contacts.FullNamePlaceholder' | translate" />
          </fieldset>
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="contact-email">{{ 'Contacts.Email' | translate }}</label>
            <input id="contact-email" type="email" class="input w-full" formControlName="email" [placeholder]="'Contacts.EmailPlaceholder' | translate" />
          </fieldset>
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="contact-phone">{{ 'Contacts.Phone' | translate }}</label>
            <input id="contact-phone" type="tel" class="input w-full" formControlName="phoneNumber" [placeholder]="'Contacts.PhonePlaceholder' | translate" />
          </fieldset>
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="contact-recipient-id">
              {{ 'Contacts.ContactCodeReadonly' | translate }}
              <span [attr.title]="(contact()?.resolved ? 'Contacts.Resolved' : 'Contacts.Unknown') | translate"><app-bool-icon [value]="contact()?.resolved" /></span>
            </label>
            <input id="contact-recipient-id" class="input w-full font-mono" readonly [value]="contact()?.recipientId ?? ''" />
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Contacts.CancelLabel' | translate }}</button>
        <button type="submit" form="contact-edit-form" class="btn btn-primary" [disabled]="saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Contacts.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class ContactEditComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `update-contact/:contactId` / `edit-contact/:contactId` route. */
  public readonly contactId = input.required<string>();

  protected readonly contact = signal<Contact | undefined>(undefined);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    salutation: [''],
    firstName: [''],
    lastName: [''],
    fullName: [''],
    email: [''],
    phoneNumber: ['']
  });

  public ngOnInit(): void {
    this._api.getContactById(this.contactId()).subscribe({
      next: contact => {
        this.contact.set(contact);
        this.form.setValue({
          salutation: contact.salutation ?? '',
          firstName: contact.firstName ?? '',
          lastName: contact.lastName ?? '',
          fullName: contact.fullName ?? '',
          email: contact.email ?? '',
          phoneNumber: contact.phoneNumber ?? ''
        });
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    const contact = this.contact();
    const value = this.form.getRawValue();
    // The API expects the whole contact back (recipient id, resolution state, preferences...) with the edited fields applied;
    // a field that was empty and stays empty is left undefined.
    const field = (name: keyof typeof value) => value[name] || (contact?.[name] === undefined ? undefined : '');
    const body = new UpdateContactRequest({
      ...contact,
      salutation: field('salutation'),
      firstName: field('firstName'),
      lastName: field('lastName'),
      fullName: field('fullName'),
      email: field('email'),
      phoneNumber: field('phoneNumber')
    });
    this.saving.set(true);
    this._api.updateContact(this.contactId(), body).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Contacts.UpdateSuccessTitle', 'Contacts.UpdateSuccessMessage', undefined, { name: body.fullName || body.email });
        this._refresh.notify('contact');
        this._refresh.notify('contacts');
        this._refresh.notify('distribution-list-contacts');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
