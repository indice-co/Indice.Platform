import { Component, ViewChild, OnInit, TemplateRef } from '@angular/core';

import { TableColumn } from '@swimlane/ngx-datatable';
import { IdentityApiService, UserInfoResultSet, UserInfo } from 'src/app/core/services/identity-api.service';
import { SearchEvent } from 'src/app/shared/components/list-view/models/search-event';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  @ViewChild('usersList', { static: true }) private _usersList: ListViewComponent;
  @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
  @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;

  constructor(private api: IdentityApiService) { }

  public count = 0;
  public rows: UserInfo[] = [];
  public columns: TableColumn[] = [];

  public ngOnInit(): void {
    this.columns = [
      { prop: 'userName', name: 'Username', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
      { prop: 'email', name: 'Email', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._usersList.emailTemplate },
      { prop: 'lastName', name: 'Last Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
      { prop: 'firstName', name: 'First Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._optionalTemplate },
      { prop: 'phoneNumber', name: 'Phone Number', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._usersList.phoneNumberTemplate },
      { prop: 'createDate', name: 'Create Date', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._usersList.dateTimeTemplate },
      { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
    ];
  }

  public getUsers(event: SearchEvent): void {
    this.api.getUsers(event.page, event.pageSize, event.sortField, event.searchTerm).subscribe((users: UserInfoResultSet) => {
      this.count = users.count;
      this.rows = users.items;
    });
  }
}
