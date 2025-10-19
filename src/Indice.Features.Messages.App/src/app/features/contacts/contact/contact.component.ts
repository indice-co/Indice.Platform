import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject, Observable, takeUntil } from 'rxjs';

import { HeaderMetaItem, ViewLayoutComponent } from '@indice/ng-components';
import { Contact } from 'src/app/core/services/messages-api.service';
import { ContactService } from './contact.service';

@Component({
  selector: 'app-contact',
  templateUrl: './contact.component.html'
})
export class ContactComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;


  private destroy$ = new Subject<void>();
  contact$: Observable<Contact>;
  isRefreshing = false;
  submitInProgress = false;
  metaItems: HeaderMetaItem[] = [];
  
  constructor(
    private _activatedRoute: ActivatedRoute,
    private _changeDetector: ChangeDetectorRef,
    private _contactService: ContactService
  ) {
    this.contact$ = _contactService.contact$;
  }

  ngOnInit(): void {
    this._contactService.setContactId(this._activatedRoute.snapshot.params['contactId']);
  }

  ngAfterViewChecked(): void {
    this._changeDetector.detectChanges();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  refresh(): void {
    this._contactService.refresh();
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
