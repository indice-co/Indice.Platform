import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { Observable, of } from 'rxjs';
import { IdentityApiService, ClaimTypeInfo } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ClaimTypeResolverService  {
    constructor(private _api: IdentityApiService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<ClaimTypeInfo> {
        const claimTypeId = route.params['id'];
        if (!claimTypeId) {
            return of(new ClaimTypeInfo());
        }
        return this._api.getClaimType(claimTypeId);
    }
}
