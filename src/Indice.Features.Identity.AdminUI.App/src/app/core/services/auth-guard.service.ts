import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root'
})
export class AuthGuardService  {
    constructor(private authService: AuthService) { }

    public canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        const observable = this.authService.isLoggedIn();
        observable.subscribe((isLoggedIn: boolean) => {
            if (!isLoggedIn) {
                this.authService.signinRedirect(state.url);
            }
        });
        return observable;
    }

    public canLoad(): Observable<boolean> | Promise<boolean> | boolean {
        return this.canActivate(undefined, undefined);
    }
}
