import { Injectable } from '@angular/core';

import { Observable, AsyncSubject } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    IdentityApiService, SingleUserInfo, RoleInfoResultSet, RoleInfo, ClaimTypeInfo, ClaimTypeInfoResultSet, UpdateUserRequest, ClaimInfo, CreateClaimRequest, BasicClaimInfo,
    UserClientInfo, UserClientInfoResultSet, UpdateUserClaimRequest, SetPasswordRequest, SetUserBlockRequest, UserLoginProviderInfo, DeviceInfo, DeviceInfoResultSet, UserLoginProviderInfoResultSet
} from 'src/app/core/services/identity-api.service';
import { ClaimType } from './details/models/claim-type.model';

@Injectable()
export class UserStore {
    private _user: AsyncSubject<SingleUserInfo>;
    private _allRoles: AsyncSubject<RoleInfo[]>;
    private _allClaims: AsyncSubject<ClaimTypeInfo[]>;
    private _userApplications: AsyncSubject<UserClientInfo[]>;
    private _userDevices: AsyncSubject<DeviceInfo[]>;
    private _userExternalLogins: AsyncSubject<UserLoginProviderInfo[]>;

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

    public updateUser(user: SingleUserInfo, requiredClaims: ClaimType[], bypassEmailAsUserNamePolicy: boolean = false): Observable<void> {
        const claims = requiredClaims ? requiredClaims.map((claim: ClaimType) => {
            return {
                type: claim.name,
                value: claim.value
            } as BasicClaimInfo;
        }) : null;
        return this._api.updateUser(user.id, {
            email: user.email,
            phoneNumber: user.phoneNumber,
            twoFactorEnabled: user.twoFactorEnabled,
            userName: user.userName,
            passwordExpirationPolicy: user.passwordExpirationPolicy,
            isAdmin: user.isAdmin,
            emailConfirmed: user.emailConfirmed,
            phoneNumberConfirmed: user.phoneNumberConfirmed,
            claims,
            bypassEmailAsUserNamePolicy
        } as UpdateUserRequest).pipe(map((updatedUser: SingleUserInfo) => {
            user.claims = [...updatedUser.claims];
            this._user.next(user);
            this._user.complete();
        }));
    }

    public deleteUser(userId: string): Observable<void> {
        return this._api.deleteUser(userId);
    }

    public blockUser(userId: string): Observable<void> {
        this.getUser(userId).subscribe((user: SingleUserInfo) => {
            user.blocked = true;
            this._user.next(user);
            this._user.complete();
        });
        return this._api.setUserBlock(userId, {
            blocked: true
        } as SetUserBlockRequest);
    }

    public unblockUser(userId: string): Observable<void> {
        this.getUser(userId).subscribe((user: SingleUserInfo) => {
            user.blocked = false;
            this._user.next(user);
            this._user.complete();
        });
        return this._api.setUserBlock(userId, {
            blocked: false
        } as SetUserBlockRequest);
    }

    public unlockUser(userId: string): Observable<void> {
        this.getUser(userId).subscribe((user: SingleUserInfo) => {
            user.lockoutEnd = null;
            user.isLocked = false;
            user.accessFailedCount = 0;
            this._user.next(user);
            this._user.complete();
        });
        return this._api.unlockUser(userId);
    }

    public resetPassword(userId: string, password: string, changePasswordAfterFirstSignIn: boolean, bypassPasswordValidation: boolean): Observable<void> {
        this.getUser(userId).subscribe((user: SingleUserInfo) => {
            this._user.next(user);
            this._user.complete();
        });
        return this._api.setPassword(userId, {
            password,
            changePasswordAfterFirstSignIn,
            bypassPasswordValidation
        } as SetPasswordRequest);
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

    public addClaim(userId: string, claim: ClaimInfo): Observable<void> {
        return this._api.addUserClaim(userId, {
            type: claim.type,
            value: claim.value
        } as CreateClaimRequest).pipe(map((createdClaim: ClaimInfo) => {
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
        } as UpdateUserClaimRequest).pipe(map(_ => {
            this.getUser(userId).subscribe((user: SingleUserInfo) => {
                const claim = user.claims.find(x => x.id === claimId);
                claim.value = value;
                this._user.next(user);
                this._user.complete();
            });
        }));
    }

    public deleteUserClaim(userId: string, claimId: number): Observable<void> {
        return this._api.deleteUserClaim(claimId, userId).pipe(map(_ => {
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

    public getUserDevices(userId: string): Observable<DeviceInfo[]> {
        if (!this._userDevices) {
            this._userDevices = new AsyncSubject<DeviceInfo[]>();
            this._api.getUserDevices(userId).subscribe((response: DeviceInfoResultSet) => {
                this._userDevices.next(response.items);
                this._userDevices.complete();
            });
        }
        return this._userDevices;
    }

    public deleteUserDevice(userId: string, deviceId: string): Observable<void> {
        return this._api.deleteUserDevice(userId, deviceId);
    }

    public getUserExternalLogins(userId: string): Observable<UserLoginProviderInfo[]> {
        if (!this._userExternalLogins) {
            this._userExternalLogins = new AsyncSubject<UserLoginProviderInfo[]>();
            this._api.getUserExternalLogins(userId).subscribe((response: UserLoginProviderInfoResultSet) => {
                this._userExternalLogins.next(response.items);
                this._userExternalLogins.complete();
            });
        }
        return this._userExternalLogins;
    }

    public deleteUserExternalLogin(userId: string, provider: string): Observable<void> {
        return this._api.deleteUserExternalLogin(userId, provider);
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
            this._api.getClaimTypes(1, 2147483647, 'name+', undefined).subscribe((response: ClaimTypeInfoResultSet) => {
                this._allClaims.next(response.items);
                this._allClaims.complete();
            });
        }
        return this._allClaims;
    }
}
