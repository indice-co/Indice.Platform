import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { map } from 'rxjs/operators';
import { Observable, from, throwError, interval } from 'rxjs';
import { UserManager, User, SignoutResponse, Profile } from 'oidc-client';
import { LoggerService } from './logger.service';
import * as app from '../models/settings';
import { RoleNames } from '../models/roles';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    constructor(private _logger: LoggerService, private _router: Router) {
        this.userManager = new UserManager(app.settings.auth_settings);
        this.userManager.clearStaleState();
        this.loadUser().subscribe();
        this.monitorTokenExpiration();
        this.userManager.events.addUserLoaded((user: User) => {
            this._logger.log('Event addUserLoaded was triggered: User is: ', user);
            this.user = user;
        });
    }

    private userManager: UserManager;
    private silentRenewInProgress = false;
    public user: User = null;

    public loadUser(): Observable<User> {
        return from(this.userManager.getUser()).pipe(map((user: User) => {
            if (user) {
                this.user = user;
                this._logger.log('User was loaded: ', user);
                this.checkUserAccess();
            } else {
                this._logger.log('User is not present.');
            }
            return user;
        }));
    }

    public isLoggedIn(): Observable<boolean> {
        return from(this.userManager.getUser()).pipe(map<User, boolean>((user: User) => {
            return user ? true : false;
        }));
    }

    public getUserProfile(): Profile {
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
        return this.getUserProfile()?.admin === true || this.hasRole(RoleNames.Administrator) || this.hasRole(RoleNames.AdminUIAdministrator);
    }

    public isAdminUIUsersReader(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIUsersReader) || this.hasRole(RoleNames.AdminUIUsersWriter);
    }

    public isAdminUIUsersWriter(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIUsersWriter);
    }

    public isAdminUIClientsReader(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIClientsReader) || this.hasRole(RoleNames.AdminUIUsersWriter);
    }

    public isAdminUIClientsWriter(): boolean {
        return this.isAdmin() || this.hasRole(RoleNames.AdminUIUsersWriter);
    }

    public getAuthorizationHeaderValue(): string {
        if (this.user) {
            return `${this.user.token_type} ${this.user.access_token}`;
        }
        this._logger.log('Method getAuthorizationHeaderValue cannot find user instance.');
        return '';
    }

    public signoutRedirect(): void {
        this.userManager.signoutRedirect().catch((error: any) => {
            this._logger.log('Method signoutRedirect returned with the following error: ', error);
        });
    }

    public signoutRedirectCallback(): Observable<SignoutResponse> {
        return from(this.userManager.signoutRedirectCallback()).pipe(map((response: SignoutResponse) => {
            this.user = null;
            return response;
        }, (error: any) => {
            this._logger.log('Method signoutRedirectCallback returned with the following error: ', error);
            throwError(error);
        }));
    }

    public signinRedirect(location: string): void {
        this.userManager
            .signinRedirect({ data: { url: location } })
            .catch((error: any) => this._logger.log(error));
    }

    public signinRedirectCallback(): Observable<User> {
        return from(this.userManager.signinRedirectCallback()).pipe(map((user: User) => {
            this._logger.log('Method signinRedirectCallback returned successfully. User is: ', user);
            this.user = user;
            return user;
        }, (error: any) => {
            this._logger.log('Method signinRedirectCallback returned with the following error: ', error);
            throwError(error);
            return null;
        }));
    }

    public signinSilent(): Observable<User> {
        return from(this.userManager.signinSilent());
    }

    public signinSilentCallback(): Observable<User> {
        return from(this.userManager.signinSilentCallback()).pipe(map((user: User) => {
            this._logger.log('Method signinSilentCallback returned successfully. User is: ', user);
            this.user = user;
            return user;
        }, (error: any) => {
            this._logger.log('Method signinSilentCallback returned with the following error: ', error);
            throwError(error);
            return null;
        }));
    }

    public checkUserAccess(): boolean {
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
        const roleClaim = this.getUserProfile()?.role as string;
        if (roleClaim && Array.isArray(roleClaim)) {
            const roles = Array.from(roleClaim);
            return roles.indexOf(roleName) !== -1;
        }
        return roleClaim === roleName;
    }

    private monitorTokenExpiration(): void {
        if (this.silentRenewInProgress) {
            return;
        }
        const monitor = interval(20000); // Check every 20 seconds.
        monitor.subscribe(_ => {
            this._logger.log(`User token ${(this.user ? `expires in ${this.user.expires_in} seconds.` : 'has expired.')}`);
            if (this.user && this.user.expires_in <= 120) { // In 120 seconds the token is about to expire.
                this._logger.log(`User token expires in ${this.user.expires_in} seconds. Application will try to renew it on the background automatically.`);
                this.silentRenewInProgress = true;
                this.signinSilent().subscribe((user: User) => {
                    this._logger.log('Method signinSilent successfully renewed token. User is: ', user);
                    this.user = user;
                    this.silentRenewInProgress = false;
                }, (error: any) => {
                    this._logger.log('Method signinSilent failed to renew token. Error is: ', error);
                    this.silentRenewInProgress = false;
                    throwError(error);
                });
            }
        });
    }
}
