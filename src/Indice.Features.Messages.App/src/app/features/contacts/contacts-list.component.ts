import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable, combineLatest, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Contact, ContactResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';

@Component({
  selector: 'app-contacts',
  templateUrl: './contacts-list.component.html'
})
export class ContactsListComponent extends BaseListComponent<Contact> implements OnInit {
  constructor(
    route: ActivatedRoute,
    private readonly _router: Router,
    private readonly _api: MessagesApiClient,
    private readonly _languages: AppLanguagesService
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'updatedAt';
    this.sortdir = 'desc';
    this.search = '';

    // Fallback initialization uses translation keys as initial labels.
    this.sortOptions = [
      new MenuOption('Contacts.SortUpdatedAtOption', 'updatedAt'),
      new MenuOption('Contacts.SortFullNameOption', 'fulname'),
      new MenuOption('Contacts.SortEmailOption', 'email'),
      new MenuOption('Contacts.SortPhoneOption', 'phone'),
      new MenuOption('Contacts.SortContactCodeOption', 'recipientId'),
      new MenuOption('Contacts.SortResolvedOption', 'resolved'),
      new MenuOption('Contacts.SortLastResolutionDateOption', 'lastResolutionDate')
    ];
  }

  public newItemLink: string | null = 'create-new-contact';
  public full = true;
  private _destroy$ = new Subject<void>();

  public override ngOnInit(): void {
    super.ngOnInit();
    // Reactive translation of sort option labels.
    const sortKeys = this.sortOptions.map(o => o.text);
    combineLatest(sortKeys.map(k => this._languages.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public loadItems(): Observable<IResultSet<Contact> | null | undefined> {
    return this._api
      .getContacts(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, undefined, undefined, undefined,
        undefined)
      .pipe(map((result: ContactResultSet) => (result as IResultSet<Contact>)));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
}
