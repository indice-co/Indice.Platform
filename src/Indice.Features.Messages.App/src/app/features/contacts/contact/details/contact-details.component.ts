import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject, Observable, takeUntil } from 'rxjs';

import { Contact } from 'src/app/core/services/messages-api.service';
import { ContactService } from '../contact.service';

@Component({
    selector: 'app-contact-details',
    templateUrl: './contact-details.component.html',
    standalone: false
})
export class ContactDetailsComponent implements OnInit, OnDestroy {

  private destroy$ = new Subject<void>();
  contact$: Observable<Contact>;
  isRefreshing = false;

  constructor(
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _contactService: ContactService
  ) {
    this.contact$ = _contactService.contact$;
}

  ngOnInit(): void {
    this._contactService.setContactId(this._activatedRoute.parent?.snapshot.params['contactId']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }


  resolve(recipientId?: string): void {
    if (!recipientId) {
      console.warn('No recipientId provided for resolve');
      return;
    }
    this.isRefreshing = true;
    this._contactService.resolveContact(recipientId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('Contact hard refresh completed');
          this.isRefreshing = false;
        },
        error: (error) => {
          console.error('Error during hard refresh:', error);
          this.isRefreshing = false;
        }
      });
  }
}
