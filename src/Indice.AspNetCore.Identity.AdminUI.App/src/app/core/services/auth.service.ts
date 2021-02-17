import { Injectable } from '@angular/core';

import { map } from 'rxjs/operators';
import { Observable, from, throwError, interval } from 'rxjs';
import { UserManager, User, SignoutResponse } from 'oidc-client';
import { LoggerService } from './logger.service';
import * as app from '../models/settings';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    constructor(private logger: LoggerService) {
        this.userManager = new UserManager(app.settings.auth_settings);
        this.monitorTokenExpiration();
        this.userManager.getUser().then(user => {
            this.user = user;
        });
        this.userManager.events.addUserLoaded((user: User) => {
            this.user = user;
        });
    }

    private userManager: UserManager;
    private silentRenewInProgress = false;
    public user: User = null;

    public isLoggedIn(): Observable<boolean> {
        return from(this.userManager.getUser()).pipe(map<User, boolean>((user: User) => {
            if (user) {
                return true;
            } else {
                return false;
            }
        }));
    }

    public async loadUser(): Promise<User> {
        return this.userManager.getUser().then((user) => {
            this.user = user;
            return user;
        });
    }

    public getEmail(): string {
        return this.user.profile.email;
    }

    public getSubjectId(): string {
        return this.user.profile.sub;
    }

    public getDisplayName(): string {
        if (this.user.profile.given_name && this.user.profile.family_name) {
            return `${this.user.profile.given_name} ${this.user.profile.family_name}`;
        }
        if (this.user.profile.email) {
            return this.user.profile.email;
        }
        if (this.user.profile.name) {
            return this.user.profile.name;
        }
        return '';
    }

    public currentUser(): User {
        return this.user;
    }

    public isAdmin(): boolean {
        return this.user.profile.admin === true;
    }

    public signinRedirect(location: string): void {
        this.userManager.signinRedirect({
            data: {
                url: location
            }
        }).catch(error => this.logger.log(error));
    }

    public async signinRedirectCallback(): Promise<User> {
        const promise = this.userManager.signinRedirectCallback();
        promise.then((user: User) => {
            this.logger.log('Method signinRedirectCallback returned successfully.');
            this.logger.log('User is:');
            this.logger.log(user);
            this.user = user;
        }, (error: any) => {
            this.logger.log('Method signinRedirectCallback returned with the following error:');
            this.logger.log(error);
            throwError(error);
        });
        return promise;
    }

    public signoutRedirect(): void {
        this.userManager.signoutRedirect().catch(error => {
            this.logger.log('Method signoutRedirect returned with the following error:');
            this.logger.log(error);
        });
    }

    public async signoutRedirectCallback(): Promise<any> {
        return this.userManager.signoutRedirectCallback().then((response: SignoutResponse) => {
            this.user = null;
        }, (error: any) => {
            this.logger.log('Method signoutRedirectCallback returned with the following error:');
            this.logger.log(error);
        });
    }

    public getAuthorizationHeaderValue(): string {
        if (this.user != null && this.user !== undefined) {
            return `${this.user.token_type} ${this.user.access_token}`;
        } else {
            this.logger.log('Method getAuthorizationHeaderValue cannot find user instance.');
            return '';
        }
    }

    public signinSilent(): Promise<User> {
        return this.userManager.signinSilent();
    }

    public signinSilentCallback(): Promise<any> {
        return this.userManager.signinSilentCallback();
    }

    private monitorTokenExpiration(): void {
        if (this.silentRenewInProgress) {
            return;
        }
        const monitor = interval(20000); // Check every 20 seconds.
        monitor.subscribe(_ => {
            this.logger.log(`User token ${(this.user ? `expires in ${this.user.expires_in} seconds.` : 'has expired.')}`);
            if (this.user && this.user.expires_in <= 120) { // In 120 seconds the token is about to expire.
                this.logger.log(`User token expires in ${this.user.expires_in} seconds. Application will try to renew it on the background automatically.`);
                this.silentRenewInProgress = true;
                this.signinSilent().then(user => {
                    this.logger.log('Method signinSilent successfully renewed token. User is:');
                    this.logger.log(user);
                    this.user = user;
                    this.silentRenewInProgress = false;
                }, (error: any) => {
                    this.logger.log('Method signinSilent failed to renew token. Error is:');
                    this.logger.log(error);
                    this.silentRenewInProgress = false;
                    throwError(error);
                });
            }
        });
    }
}
