import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject, Observable, takeUntil, switchMap, shareReplay, take } from 'rxjs';

import { ContactPreference, ContactChannelOption, ContactChannelKind, Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactService } from '../contact.service';

@Component({
  selector: 'app-contact-preferences',
  templateUrl: './contact-preferences.component.html'
})
export class ContactPreferencesComponent implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();
  contact$: Observable<Contact>;
  preference$: Observable<ContactPreference>;
  isRefreshing = false;

  recepientPreference: ContactPreference | undefined;
  protected ContactChannelKindEnum = ContactChannelKind;

  constructor(
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _api: MessagesApiClient,
    private readonly _contactService: ContactService
  ) {
    this.contact$ = _contactService.contact$;
    this.preference$ = this.contact$.pipe(
      take(1),
      switchMap(contact => this._api.getPreferences(contact.id!)),
      takeUntil(this.destroy$),
      shareReplay(1)
    );
  }

  ngOnInit(): void {
    this._contactService.setContactId(this._activatedRoute.parent?.snapshot.params['contactId']);
  }

  CheckReceivePreference(communicationPreferences: ContactChannelOption[], option: ContactChannelKind): boolean {
    return communicationPreferences.findIndex(obj => obj.kind === option && obj.include) >= 0;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
