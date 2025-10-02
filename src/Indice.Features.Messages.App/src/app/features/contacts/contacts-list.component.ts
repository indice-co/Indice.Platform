import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Contact, ContactResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-contacts',
    templateUrl: './contacts-list.component.html',
    standalone: false
})
export class ContactsListComponent extends BaseListComponent<Contact> implements OnInit {
  constructor(
    route: ActivatedRoute,
    private readonly _router: Router,
    private readonly _api: MessagesApiClient
    
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'updatedAt';
    this.sortdir = 'asc';
    this.search = '';
    this.sortOptions = [
      new MenuOption('Τροποποιήθηκε', 'updatedAt'),
      new MenuOption('Ονοματεπώνυμο', 'fulname'),
      new MenuOption('e-mail', 'email'),
      new MenuOption('Τηλέφωνο', 'phone'),
      new MenuOption('Κωδικός', 'recipientId'),
      new MenuOption('Resolved', 'resolved'),
      new MenuOption('Last Resolution', 'lastResolutionDate'),
    ];
  } 

  public newItemLink: string | null = 'create-new-contact';
  public full = true;

  public override ngOnInit(): void {
    super.ngOnInit();
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
