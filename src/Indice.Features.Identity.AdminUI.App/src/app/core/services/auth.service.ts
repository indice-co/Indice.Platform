import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { map } from 'rxjs/operators';
import { Observable, from, throwError } from 'rxjs';
import { UserManager, User, SignoutResponse, UserProfile, Log } from 'oidc-client-ts';
import { LoggerService } from './logger.service';
import * as app from '../models/settings';
import { RoleNames } from '../models/roles';

/**
 * https://github.com/IdentityModel/oidc-client-js/wiki
 */
@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private _userManager: UserManager;

    constructor(private _logger: LoggerService, private _router: Router) {
        if (!app.settings.production) {
            Log.setLogger(console);
            Log.setLevel(Log.INFO);
        }
        this._userManager = new UserManager(app.settings.auth_settings);
        this.loadUser().subscribe();
        this._userManager.events.addUserLoaded((user: User) => {
            this.user = user;
        });
        this._userManager.events.addAccessTokenExpiring(_ => {
            this.signinSilent().subscribe((user: User) => {
                this.user = user;
            }, (error: any) => {
                throwError(error);
            });
        });
        this._userManager.events.addUserSignedOut(() => {
            this.removeUser();
        });
    }

    public user: User = null;

    public loadUser(): Observable<User> {
        return from(this._userManager.getUser()).pipe(map((user: User) => {
            if (user) {
                this.user = user;
                this.userHasAccess();
            } else {
                this._logger.log('User is not present.');
            }
            return user;
        }));
    }

    public isLoggedIn(): Observable<boolean> {
        return from(this._userManager.getUser()).pipe(map<User, boolean>((user: User) => {
            return user ? true : false;
        }));
    }

    public getUserProfile(): UserProfile {
        return this.user?.profile;
    }

    public getEmail(): string {
        return this.getUserProfile()?.email;
    }

    public getSubjectId(): string {
        return this.getUserProfile()?.sub;
    }

    public getDisplayName(): string {
        const userProfile = this.getUserProfile();
        if (userProfile?.given_name && userProfile?.family_name) {
            return `${userProfile.given_name} ${userProfile.family_name}`;
        }
        if (this.getEmail()) {
            return this.getEmail();
        }
        if (this.user?.profile?.name) {
            return userProfile.name;
        }
        return '';
    }

    public getCurrentUser(): User {
        return this.user;
    }

    public isAdmin(): boolean {
        return this.getUserProfile()?.['admin'] === true || this.hasRole(RoleNames.Administrator) || this.hasRole(RoleNames.AdminUIAdministrator);
    }

    public isAdminUIUsersReader(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIUsersReader) || this.hasRole(RoleNames.AdminUIUsersWriter);
    }

    public isAdminUIUsersWriter(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIUsersWriter);
    }

    public isAdminUIClientsReader(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIClientsReader) || this.hasRole(RoleNames.AdminUIClientsWriter);
    }

    public isAdminUIClientsWriter(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIClientsWriter);
    }

    public getAuthorizationHeaderValue(): string {
        if (this.user) {
            return `${this.user.token_type} ${this.user.access_token}`;
        }
        this._logger.log('Method getAuthorizationHeaderValue cannot find user instance.');
        return '';
    }

    public signoutRedirect(): void {
        this._userManager.signoutRedirect();
    }

    public removeUser(): void {
        this._userManager.removeUser().then(() => {
            this._router.navigateByUrl('/home');
        });
    }

    public signoutRedirectCallback(): Observable<SignoutResponse> {
        return from(this._userManager.signoutRedirectCallback()).pipe(map((response: SignoutResponse) => {
            this.user = null;
            return response;
        }, (error: any) => {
            throwError(error);
        }));
    }

    public signinRedirect(location: string): void {
        this._userManager
            .signinRedirect({ url_state:  location } )
            .catch((error: any) => this._logger.log(error));
    }

    public signinRedirectCallback(): Observable<User> {
        return from(this._userManager.signinRedirectCallback()).pipe(map((user: User) => {
            this.user = user;
            return user;
        }, (error: any) => {
            throwError(error);
            return null;
        }));
    }

    public signinSilent(): Observable<User> {
        return from(this._userManager.signinSilent());
    }

    public signinSilentCallback(): Observable<void> {
        return from(this._userManager.signinSilentCallback()).pipe(map(() => {
            
        }, (error: any) => {
            throwError(error);
            return null;
        }));
    }

    public userHasAccess(): boolean {
        const user = this.getCurrentUser();
        if (!user) {
            return true;
        }
        if (!this.isAdmin() && !this.isAdminUIUsersReader() && !this.isAdminUIClientsReader()) {
            this._logger.log('User is not authorized to access Admin UI. Access is forbidden.');
            this._router.navigate(['/forbidden']);
            return false;
        }
        return true;
    }

    private hasRole(roleName: string): boolean {
        const roleClaim = this.getUserProfile()?.['role'] as string;
        if (roleClaim && Array.isArray(roleClaim)) {
            const roles = Array.from(roleClaim);
            return roles.indexOf(roleName) !== -1;
        }
        return roleClaim === roleName;
    }
}
