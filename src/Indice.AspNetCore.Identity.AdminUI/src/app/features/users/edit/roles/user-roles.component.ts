import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { RoleInfo, SingleUserInfo } from 'src/app/core/services/identity-api.service';
import { UserStore } from '../user-store.service';
import { TransferListsComponent } from 'src/app/shared/components/transfer-lists/transfer-lists.component';

@Component({
    selector: 'app-user-roles',
    templateUrl: './user-roles.component.html'
})
export class UserRolesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addUserRoleSubscription: Subscription;
    private _removeUserRoleSubscription: Subscription;
    private _userId = '';

    constructor(private _route: ActivatedRoute, private _userStore: UserStore) { }

    public availableRoles: RoleInfo[];
    public userRoles: RoleInfo[];

    public ngOnInit(): void {
        this._userId = this._route.parent.snapshot.params.id;
        const getUser = this._userStore.getUser(this._userId);
        const getRoles = this._userStore.getAllRoles();
        this._getDataSubscription = forkJoin([getUser, getRoles]).pipe(map((responses: [SingleUserInfo, RoleInfo[]]) => {
            return {
                user: responses[0],
                roles: responses[1]
            };
        })).subscribe((result: { user: SingleUserInfo, roles: RoleInfo[] }) => {
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
        this._addUserRoleSubscription = this._userStore.addUserRole(this._userId, role).subscribe();
    }

    public removeRole(role: RoleInfo): void {
        this._removeUserRoleSubscription = this._userStore.deleteUserRole(this._userId, role).subscribe();
    }
}
