import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { Observable, of } from 'rxjs';
import { IdentityApiService, RoleInfo, AppSettingInfo } from 'src/app/core/services/identity-api.service';

@Injectable()
export class SettingResolverService implements Resolve<AppSettingInfo> {
    constructor(private api: IdentityApiService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<AppSettingInfo> {
        const key = route.params.id;
        if (!key) {
            return of(new AppSettingInfo());
        }
        return this.api.getSetting(key);
    }
}
