import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { Observable, of } from 'rxjs';
import { IdentityApiService, RoleInfo } from 'src/app/core/services/identity-api.service';

@Injectable()
export class RoleResolverService  {
    constructor(private api: IdentityApiService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<RoleInfo> {
        const roleId = route.params['id'];
        if (!roleId) {
            return of(new RoleInfo());
        }
        return this.api.getRole(roleId);
    }
}
