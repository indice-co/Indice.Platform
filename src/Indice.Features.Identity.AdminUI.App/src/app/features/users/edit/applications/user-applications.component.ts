import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { UserClientInfo } from 'src/app/core/services/identity-api.service';
import { UserStore } from '../user-store.service';
import { AuthService } from "src/app/core/services/auth.service";
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';

@Component({
    selector: 'app-user-applications',
    templateUrl: './user-applications.component.html',
    standalone: false
})
export class UserApplicationsComponent implements OnInit, OnDestroy {
    @ViewChild('userApplicationsList', { static: true }) public _userApplicationsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('userAppKeyTemplate', { static: true }) private _keyTemplate: TemplateRef<HTMLElement>;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;
    private _userId: string;
    public canEditUser: boolean;

    constructor(
        private _userStore: UserStore,
        private _authService: AuthService,
        private _route: ActivatedRoute,
        private _modalService: NgbModal,
        private _toast: ToastService
    ) { }

    public columns: TableColumn[] = [];
    public rows: UserClientInfo[] = [];
    public selectedUserClient: UserClientInfo;

    public ngOnInit(): void {
        this.canEditUser = this._authService.isAdminUIUsersWriter();
        this.columns = [
            { prop: 'clientId', name: 'App Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._keyTemplate },
            { prop: 'clientName', name: 'App Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        this._userId = this._route.parent.snapshot.params['id'];
        this._getDataSubscription = this._userStore.getUserApplications(this._userId).subscribe((userApplications: UserClientInfo[]) => this.rows = userApplications);
    }

    public ngOnDestroy(): void {
        this._getDataSubscription?.unsubscribe();
    }

    public showDetails(client: UserClientInfo, content: any): void {
        this.selectedUserClient = client;
        this._modalService.open(content);
    }

    public showDeleteAlert(client: UserClientInfo): void {
        this.selectedUserClient = client;
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._userStore.revokeUserApplicationAccess(this._userId, this.selectedUserClient.clientId).subscribe(_ => {
            this.rows = [...this.rows.filter(x => x.clientId !== this.selectedUserClient.clientId)];
            this._toast.showSuccess(`Login provider '${this.selectedUserClient.clientName || this.selectedUserClient.clientId}' was successfully removed from user.`);
        });
    }
}
