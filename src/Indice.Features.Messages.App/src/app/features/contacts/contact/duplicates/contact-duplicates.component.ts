import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ToasterService, ToastType } from '@indice/ng-components';
import { catchError, map, Observable, of, startWith, Subscription, tap } from 'rxjs';
import { Contact, ContactResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { settings } from '../../../../core/models/settings';


@Component({
  selector: 'app-contact-duplicates',
  templateUrl: './contact-duplicates.component.html',
  styles: ['']
})
export class ContactDuplicatesComponent implements OnInit, AfterViewInit, OnDestroy {
  private _getContactSubscription!: Subscription;
  private _mergeContactsSubscription!: Subscription;
  private _contactId: string = '';
  public avatarOrigin = (settings.api_url || "").substring(0, 4) === "http" ? new URL(settings.api_url).origin : "";
  public mergedContactsIds!: Set<string>;

  constructor(
    private _changeDetector: ChangeDetectorRef,
    private _api: MessagesApiClient,
    private _router: Router,
    private _activatedRoute: ActivatedRoute,

    @Inject(ToasterService) private _toaster: ToasterService
  ) { }

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  public submitInProgress = false;
  public model = new Contact();
  duplicatesState$!: Observable<{
    loading: boolean; contacts: ContactResultSet | null}>;
  loader = true;


  public ngOnInit(): void {
    this._contactId = this._activatedRoute.snapshot.params['contactId'];
    this._getContactSubscription = this
      ._api
      .getContactById(this._contactId)
      .subscribe((contact: Contact) => this.model = contact);

    this.duplicatesState$ = this._api.getDuplicateContacts(this._contactId).pipe(
      map(contact => ({ loading: false, contacts: contact })),
      startWith({ loading: true, contacts: null }), 
      catchError(() => of({ loading: false, contacts: null }))
    );
    this.mergedContactsIds = new Set<string>();
  }

  public ngAfterViewInit(): void {
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {
    this._getContactSubscription?.unsubscribe();
    this._mergeContactsSubscription?.unsubscribe();
  }

  //merge
  public onSubmitSelected(): void {
    this.submitInProgress = true;
    this._mergeContactsSubscription = this._api
      .mergeContacts(this._contactId, Array.from(this.mergedContactsIds))
      .subscribe({
        next: () => {
          this.submitInProgress = false;
          this._toaster.show(ToastType.Success, 'Επιτυχής συγχόνευση', `Τα διπλότυπα της επαφής '${this.model.fullName || this.model.email}' συγχονεύτηκαν με επιτυχία.`);
          this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['contacts', this._contactId, 'contact-details']));
        }
      });
  }

  public toggleSelection(id: string, checked: boolean) {
    if (checked) {
      this.mergedContactsIds.add(id);
    } else {
      this.mergedContactsIds.delete(id);
    }
  }

}
