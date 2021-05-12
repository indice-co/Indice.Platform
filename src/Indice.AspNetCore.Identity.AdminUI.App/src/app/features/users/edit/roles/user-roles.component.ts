import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { RoleInfo, SingleUserInfo } from 'src/app/core/services/identity-api.service';
import { UserStore } from '../user-store.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-user-roles',
    templateUrl: './user-roles.component.html'
})
export class UserRolesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addUserRoleSubscription: Subscription;
    private _removeUserRoleSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute, 
        private _userStore: UserStore, 
        private _toast: ToastService, 
        private _authService: AuthService
    ) { }

    public availableRoles: RoleInfo[];
    public userRoles: RoleInfo[];
    public user: SingleUserInfo = new SingleUserInfo();
    public currentUserId: string;
    public canEditUser: boolean;

    public ngOnInit(): void {
        this.canEditUser = this._authService.isAdminUIClientsWriter();
        const userId = this._route.parent.snapshot.params.id;
        this.currentUserId = this._authService.getSubjectId();
        const getUser = this._userStore.getUser(userId);
        const getRoles = this._userStore.getAllRoles();
        this._getDataSubscription = forkJoin([getUser, getRoles]).pipe(map((responses: [SingleUserInfo, RoleInfo[]]) => {
            return {
                user: responses[0],
                roles: responses[1]
            };
        })).subscribe((result: { user: SingleUserInfo, roles: RoleInfo[] }) => {
            this.user = result.user;
            const userRoles = result.user.roles;
            const allRoles = result.roles;
            this.availableRoles = allRoles.filter(x => !userRoles.includes(x.name));
            this.userRoles = allRoles.filter(x => userRoles.includes(x.name));
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addUserRoleSubscription) {
            this._addUserRoleSubscription.unsubscribe();
        }
        if (this._removeUserRoleSubscription) {
            this._removeUserRoleSubscription.unsubscribe();
        }
    }

    public addRole(role: RoleInfo): void {
        this._addUserRoleSubscription = this._userStore.addUserRole(this.user.id, role).subscribe(_ => {
            this._toast.showSuccess(`Role '${role.name}' was successfully added to user '${this.user.email}'.`);
        });
    }

    public removeRole(role: RoleInfo): void {
        this._removeUserRoleSubscription = this._userStore.deleteUserRole(this.user.id, role).subscribe(_ => {
            this._toast.showSuccess(`Role '${role.name}' was successfully removed from user '${this.user.email}'.`);
        });
    }

    public toggleAdmin(): void {
        this.user.isAdmin = !this.user.isAdmin;
        this._userStore.updateUser(this.user, null).subscribe(_ => {
            this._toast.showSuccess(`User '${this.user.email}' was updated successfully.`);
        });
    }
}
