import { Component, ViewChild, OnInit, TemplateRef } from "@angular/core";

import { TableColumn } from "@swimlane/ngx-datatable";
import {
  IdentityApiService,
  UserInfoResultSet,
  UserInfo,
} from "src/app/core/services/identity-api.service";
import { SearchEvent } from "src/app/shared/components/list-view/models/search-event";
import { ListViewComponent } from "src/app/shared/components/list-view/list-view.component";
import { AuthService } from "src/app/core/services/auth.service";
import { UiFeaturesService } from "src/app/core/services/ui-features.service";
import { Observable, map } from "rxjs";

@Component({
  selector: "app-users",
  templateUrl: "./users.component.html",
})
export class UsersComponent implements OnInit {
  @ViewChild("usersList", { static: true })
  private _usersList: ListViewComponent;
  @ViewChild("actionsTemplate", { static: true })
  private _actionsTemplate: TemplateRef<HTMLElement>;
  @ViewChild("optionalTemplate", { static: true })
  private _optionalTemplate: TemplateRef<HTMLElement>;

  constructor(
    private api: IdentityApiService,
    private _authService: AuthService,
    private uiFeaturesService: UiFeaturesService
  ) {}

  public count = 0;
  public rows: UserInfo[] = [];
  public columns: Observable<TableColumn[]> = undefined;
  private _columns: TableColumn[] = [];
  public canEditUser: boolean;

  public ngOnInit(): void {
    this.canEditUser = this._authService.isAdminUIUsersWriter();
    this._columns = [
      {
        prop: "userName",
        name: "Username",
        draggable: false,
        canAutoResize: true,
        width: 227,
        sortable: true,
        resizeable: true,
        frozenLeft: true,
        cellTemplate: this._usersList.usernameOrEmailTemplate,
      },
      {
        prop: "lastName",
        name: "Last Name",
        draggable: false,
        canAutoResize: true,
        sortable: true,
        resizeable: true,
        cellTemplate: this._optionalTemplate,
      },
      {
        prop: "firstName",
        name: "First Name",
        draggable: false,
        canAutoResize: true,
        sortable: true,
        resizeable: true,
        cellTemplate: this._optionalTemplate,
      },
      {
        prop: "phoneNumber",
        name: "Phone Number",
        draggable: false,
        canAutoResize: true,
        sortable: true,
        resizeable: true,
        cellTemplate: this._usersList.phoneNumberTemplate,
      },
      {
        prop: "createDate",
        name: "Create Date",
        draggable: false,
        canAutoResize: false,
        sortable: true,
        resizeable: false,
        cellTemplate: this._usersList.dateTimeTemplate,
        width: 200,
      },
      {
        prop: "lastSignInDate",
        name: "Last Sign In",
        draggable: false,
        canAutoResize: false,
        sortable: true,
        resizeable: false,
        cellTemplate: this._usersList.dateTimeTemplate,
        width: 200,
      },
      {
        prop: "isAdmin",
        name: "Admin",
        draggable: false,
        canAutoResize: false,
        sortable: true,
        resizeable: false,
        cellTemplate: this._usersList.booleanTemplate,
        width: 80,
      },
      {
        prop: "id",
        name: "Actions",
        draggable: false,
        canAutoResize: false,
        sortable: false,
        resizeable: false,
        cellTemplate: this._actionsTemplate,
        cellClass: "d-flex align-items-center",
        width: 100,
      },
    ];
    
    this.columns = this.uiFeaturesService.getUiFeatures()
    .pipe(map(result => {
      if (!result.emailAsUserName) {
        this._columns.splice(1, 0, {
          prop: "email",
          name: "Email",
          draggable: false,
          canAutoResize: true,
          sortable: true,
          resizeable: true,
          cellTemplate: this._usersList.emailTemplate,
        });
        this._columns[0].cellTemplate = this._usersList.usernameTemplate;
      }
      return this._columns;
    }));
  }

  public getUsers(event: SearchEvent): void {
    this.api
      .getUsers(event.page, event.pageSize, event.sortField, event.searchTerm)
      .subscribe((users: UserInfoResultSet) => {
        this.count = users.count;
        this.rows = users.items;
      });
  }
}
