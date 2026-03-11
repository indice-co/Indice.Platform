import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { CellContext, TableColumn } from '@swimlane/ngx-datatable';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ServerSideSessionInfo } from 'src/app/core/services/identity-api.service';
import { UserStore } from '../user-store.service';
import { AuthService } from "src/app/core/services/auth.service";
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';

@Component({
    selector: 'app-user-sessions',
    templateUrl: './user-sessions.component.html',
    standalone: false
})
export class UserSessionsComponent implements OnInit, OnDestroy {
    @ViewChild('userSessionsList', { static: true }) public _userSessionsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<CellContext<any>>;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    @ViewChild('dateTemplate', { static: true }) private _dateTemplate: TemplateRef<CellContext<any>>;

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
    public rows: ServerSideSessionInfo[] = [];
    public selectedSession: ServerSideSessionInfo;

    public ngOnInit(): void {
        this.canEditUser = this._authService.isAdminUIUsersWriter();
        this.columns = [
            { prop: 'sessionId', name: 'Session Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._userSessionsList.keyTemplate },
            { prop: 'created', name: 'Created At', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._dateTemplate },
            { prop: 'expires', name: 'Expires At', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._dateTemplate },
            { prop: 'key', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        this._userId = this._route.parent.snapshot.params['id'];
        this._getDataSubscription = this._userStore.getUserSessions(this._userId).subscribe((userSessions: ServerSideSessionInfo[]) => this.rows = userSessions);
    }

    public ngOnDestroy(): void {
        this._getDataSubscription?.unsubscribe();
    }

    public showDetails(session: ServerSideSessionInfo, content: any): void {
        this.selectedSession = session;
        this._modalService.open(content);
    }

    public showDeleteAlert(session: ServerSideSessionInfo): void {
        this.selectedSession = session;
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._userStore.removeUserSession(this._userId, this.selectedSession.sessionId).subscribe(_ => {
            this.rows = [...this.rows.filter(x => x.sessionId !== this.selectedSession.sessionId)];
            this._toast.showSuccess(`Session '${this.selectedSession.displayName || this.selectedSession.sessionId}' was successfully removed from user.`);
        });
    }
}
