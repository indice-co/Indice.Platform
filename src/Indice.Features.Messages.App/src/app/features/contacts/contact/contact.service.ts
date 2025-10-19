import { Injectable } from '@angular/core';
import { Observable, Subject, merge, BehaviorSubject, EMPTY } from 'rxjs';
import { switchMap, shareReplay, map, tap, distinctUntilChanged, filter } from 'rxjs/operators';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  private contactId$ = new BehaviorSubject<string>('');
  private refresh$ = new Subject<void>();
  private instantceId = Math.random();

  // The main contact stream following Thoughtram pattern
  public contact$: Observable<Contact> = merge(
    this.contactId$.pipe(distinctUntilChanged()),
    this.refresh$.pipe(map(() => this.currentContactId))
  ).pipe(
    filter(contactId => !!contactId), // Only proceed if contactId is not empty
    switchMap(contactId => {
      console.log(`[Service ${this.instantceId}] Loading contact: ${contactId}`);
      return this.api.getContactById(contactId);
    }),
    shareReplay({ bufferSize: 1, refCount: false })
  );

  private currentContactId: string = '';

  constructor(private readonly api: MessagesApiClient) {}

  /**
   * Sets the contact ID to load
   */
  setContactId(contactId: string): void {
    console.log(`[Service ${this.instantceId}] try set contact ID to: ${contactId}`);
    this.currentContactId = contactId;
    this.contactId$.next(contactId);
  }

  /**
   * Triggers a refresh of the current contact from cache
   */
  refresh(): void {
    if (this.currentContactId) {
      this.refresh$.next();
    }
  }

  /**
   * Forces a hard refresh from the external system and then refreshes the local data
   */
  resolveContact(recipientId: string): Observable<void> {
    return this.api.resolveContact(recipientId).pipe(
      tap(() => {
        // there is not way of knowing for sure if the contact was updated or not but since
        // the we trigger the server refresh using the recipientId (a correlation id that resides on the current contact)
        // probably the current contact is the one that was refreshed. So we trigger a local refresh/reload to get the latest data.
        this.refresh$.next();
      })
    );
  }

  /**
   * Forces a hard refresh from the external system (fire-and-forget)
   * Automatically refreshes local data when complete
   */
  resolveContactAsync(recipientId: string): void {
    this.resolveContact(recipientId).subscribe({
      error: (err) => console.error('Contact refresh failed:', err)
    });
  }

  /**
   * Gets the current contact ID
   */
  getCurrentContactId(): string {
    return this.currentContactId;
  }
}
