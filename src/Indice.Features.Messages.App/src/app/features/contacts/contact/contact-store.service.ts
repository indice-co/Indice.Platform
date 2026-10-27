import { Injectable, inject, signal } from '@angular/core';
import { Observable, Subscription, tap } from 'rxjs';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';

/**
 * State of the contact shown by the contact detail page. Provided by `ContactComponent`, so the page and its tabs
 * share one instance.
 */
@Injectable()
export class ContactStore {
  private readonly _api = inject(MessagesApiClient);
  private _request?: Subscription;

  public readonly contactId = signal('');
  public readonly contact = signal<Contact | undefined>(undefined);
  public readonly loading = signal(false);
  public readonly failed = signal(false);

  public load(contactId: string): void {
    if (contactId !== this.contactId()) {
      this.contact.set(undefined);
    }
    this.contactId.set(contactId);
    this.reload();
  }

  public reload(): void {
    const id = this.contactId();
    if (!id) {
      return;
    }
    this._request?.unsubscribe();
    this.loading.set(true);
    this.failed.set(false);
    this._request = this._api.getContactById(id).subscribe({
      next: contact => {
        this.contact.set(contact);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      }
    });
  }

  /**
   * Asks the server to refresh the contact from the external system by its recipient id, then reloads it (the
   * recipient id is a correlation id of the current contact, so it is most probably the one that was refreshed).
   */
  public resolve(recipientId: string): Observable<void> {
    return this._api.resolveContact(recipientId).pipe(tap(() => this.reload()));
  }
}
