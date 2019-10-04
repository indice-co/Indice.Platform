import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { IdentityApiService, IdentityResourceInfo, ClaimTypeInfo, ClaimTypeInfoResultSet } from 'src/app/core/services/identity-api.service';

@Injectable()
export class IdentityResourceStore {
    private _identityResource: AsyncSubject<IdentityResourceInfo>;
    private _allClaims: AsyncSubject<ClaimTypeInfo[]>;

    constructor(private _api: IdentityApiService) { }

    public getIdentityResource(resourceId: number): Observable<IdentityResourceInfo> {
        if (!this._identityResource) {
            this._identityResource = new AsyncSubject<IdentityResourceInfo>();
            this._api.getIdentityResource(resourceId).subscribe((user: IdentityResourceInfo) => {
                this._identityResource.next(user);
                this._identityResource.complete();
            });
        }
        return this._identityResource;
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
