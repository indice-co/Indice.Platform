import { Injectable } from '@angular/core';

import { Observable, AsyncSubject } from 'rxjs';
import {
    IdentityApiService, SingleUserInfo, RoleInfoResultSet, RoleInfo, ClaimTypeInfo, ClaimTypeInfoResultSet, UpdateUserRequest, ClaimInfo, CreateUserClaimRequest, BasicClaimInfo,
    UserClientInfo, UserClientInfoResultSet, UpdateUserClaimRequest
} from 'src/app/core/services/identity-api.service';
import { ClaimType } from './details/models/claim-type.model';
import { map } from 'rxjs/operators';

@Injectable()
export class UserStore {
    private _user: AsyncSubject<SingleUserInfo>;
    private _allRoles: AsyncSubject<RoleInfo[]>;
    private _allClaims: AsyncSubject<ClaimTypeInfo[]>;
    private _userApplications: AsyncSubject<UserClientInfo[]>;

    constructor(private _api: IdentityApiService) { }

    public getUser(userId: string): Observable<SingleUserInfo> {
        if (!this._user) {
            this._user = new AsyncSubject<SingleUserInfo>();
            this._api.getUser(userId).subscribe((user: SingleUserInfo) => {
                this._user.next(user);
                this._user.complete();
            });
        }
        return this._user;
    }

    public updateUser(user: SingleUserInfo, requiredClaims: ClaimType[]): Observable<void> {
        const claims = requiredClaims.map((claim: ClaimType) => {
            return {
                type: claim.name,
                value: claim.value
            } as BasicClaimInfo;
        });
        return this._api.updateUser(user.id, {
            email: user.email,
            lockoutEnabled: user.lockoutEnabled,
            lockoutEnd: user.lockoutEnd,
            phoneNumber: user.phoneNumber,
            twoFactorEnabled: user.twoFactorEnabled,
            userName: user.userName,
            claims
        } as UpdateUserRequest).pipe(map((updatedUser: SingleUserInfo) => {
            user.claims = [...updatedUser.claims];
            this._user.next(user);
            this._user.complete();
        }));
    }

    public addUserRole(userId: string, role: RoleInfo): Observable<void> {
        this.getUser(userId).subscribe((user: SingleUserInfo) => {
            user.roles.push(role.name);
            this._user.next(user);
            this._user.complete();
        });
        return this._api.addUserRole(userId, role.id);
    }

    public deleteUserRole(userId: string, role: RoleInfo): Observable<void> {
        this.getUser(userId).subscribe((user: SingleUserInfo) => {
            const index = user.roles.indexOf(role.name, 0);
            if (index > -1) {
                user.roles.splice(index, 1);
            }
            this._user.next(user);
            this._user.complete();
        });
        return this._api.deleteUserRole(userId, role.id);
    }

    public addUserClaim(userId: string, claim: ClaimInfo): Observable<void> {
        return this._api.addUserClaim(userId, {
            claimType: claim.type,
            claimValue: claim.value
        } as CreateUserClaimRequest).pipe(map((createdClaim: ClaimInfo) => {
            this.getUser(userId).subscribe((user: SingleUserInfo) => {
                user.claims.push(createdClaim);
                this._user.next(user);
                this._user.complete();
            });
        }));
    }

    public updateUserClaim(userId: string, claimId: number, value: string): Observable<void> {
        return this._api.updateUserClaim(userId, claimId, {
            claimValue: value
        } as UpdateUserClaimRequest).pipe(map((updatedClaim: ClaimInfo) => {
            this.getUser(userId).subscribe((user: SingleUserInfo) => {
                const claim = user.claims.find(x => x.id === claimId);
                claim.value = value;
                this._user.next(user);
                this._user.complete();
            });
        }));
    }

    public deleteUserClaim(userId: string, claimId: number): Observable<void> {
        return this._api.deleteUserClaim(userId, claimId).pipe(map(_ => {
            this.getUser(userId).subscribe((user: SingleUserInfo) => {
                const claim = user.claims.find(x => x.id === claimId);
                const index = user.claims.indexOf(claim, 0);
                if (index > -1) {
                    user.claims.splice(index, 1);
                }
                this._user.next(user);
                this._user.complete();
            });
        }));
    }

    public getUserApplications(userId: string): Observable<UserClientInfo[]> {
        if (!this._userApplications) {
            this._userApplications = new AsyncSubject<UserClientInfo[]>();
            this._api.getUserApplications(userId).subscribe((response: UserClientInfoResultSet) => {
                this._userApplications.next(response.items);
                this._userApplications.complete();
            });
        }
        return this._userApplications;
    }

    public getAllRoles(): Observable<RoleInfo[]> {
        if (!this._allRoles) {
            this._allRoles = new AsyncSubject<RoleInfo[]>();
            this._api.getRoles(1, 2147483647, 'name+', undefined).subscribe((response: RoleInfoResultSet) => {
                this._allRoles.next(response.items);
                this._allRoles.complete();
            });
        }
        return this._allRoles;
    }

    public getAllClaims(): Observable<ClaimTypeInfo[]> {
        if (!this._allClaims) {
            this._allClaims = new AsyncSubject<ClaimTypeInfo[]>();
            this._api.getClaimTypes(undefined, 1, 2147483647, 'name+', undefined).subscribe((response: ClaimTypeInfoResultSet) => {
                this._allClaims.next(response.items);
                this._allClaims.complete();
            });
        }
        return this._allClaims;
    }
}
