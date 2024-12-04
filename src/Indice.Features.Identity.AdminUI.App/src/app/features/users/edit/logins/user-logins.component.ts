import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { UserLoginProviderInfo } from 'src/app/core/services/identity-api.service';
import { UserStore } from '../user-store.service';
import { AuthService } from 'src/app/core/services/auth.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';

@Component({
    selector: 'app-user-logins',
    templateUrl: './user-logins.component.html'
})
export class UserLoginsComponent implements OnInit, OnDestroy {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('providerNameTemplate', { static: true }) private _providerNameTemplate: TemplateRef<HTMLElement>;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    @ViewChild('userApplicationsList', { static: true }) public userApplicationsList: ListViewComponent;
    private _getDataSubscription: Subscription;
    private _providerToDelete: UserLoginProviderInfo;
    private _userId: string;

    constructor(
        private _userStore: UserStore,
        private _route: ActivatedRoute,
        private _authService: AuthService,
        private _toast: ToastService
    ) { }

    public columns: TableColumn[] = [];
    public rows: UserLoginProviderInfo[] = [];
    public canEditUser: boolean;

    public ngOnInit(): void {
        this.canEditUser = this._authService.isAdminUIUsersWriter();
        this.columns = [
            { prop: 'name', name: 'Provider', draggable: false, canAutoResize: true, sortable: false, resizeable: true, cellTemplate: this._providerNameTemplate, width:50  },
            { prop: 'displayName', name: 'Display name', draggable: false, canAutoResize: true, sortable: false, resizeable: true },
            { prop: 'key', name: 'Provider Key', draggable: false, canAutoResize: true, sortable: false, resizeable: false }
        ];
        if (this.canEditUser) {
            this.columns.push({ prop: 'key', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center', width:50 })
        }
        this._userId = this._route.parent.snapshot.params['id'];
        this._getDataSubscription = this._userStore.getUserExternalLogins(this._userId).subscribe((userExternalLogins: UserLoginProviderInfo[]) => {
            this.rows = userExternalLogins;
        });
    }

    public showDeleteAlert(key: string): void {
        this._providerToDelete = this.rows.find(x => x.key === key);
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._userStore.deleteUserExternalLogin(this._userId, this._providerToDelete.name).subscribe(_ => {
            this.rows = [...this.rows.filter(x => x.name !== this._providerToDelete.name)];
            this._toast.showSuccess(`Login provider '${this._providerToDelete.name}' was successfully removed from user.`);
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }
}
