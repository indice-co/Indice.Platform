import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { ApiResourceInfo, IdentityApiService, ScopeInfo, ClaimTypeInfo, ClaimTypeInfoResultSet } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ApiResourceStore {
    private _apiResource: AsyncSubject<ApiResourceInfo>;
    private _allClaims: AsyncSubject<ClaimTypeInfo[]>;

    constructor(private _api: IdentityApiService) { }

    public getApiResource(apiResourceId: number): Observable<ApiResourceInfo> {
        if (!this._apiResource) {
            this._apiResource = new AsyncSubject<ApiResourceInfo>();
            this._api.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
                apiResource.scopes = apiResource.scopes.sort((x: ScopeInfo, y: ScopeInfo) => (x.name > y.name ? 1 : -1));
                apiResource.scopes.forEach((value: ScopeInfo) => {
                    (value as any).isOpen = false;
                });
                this._apiResource.next(apiResource);
                this._apiResource.complete();
            });
        }
        return this._apiResource;
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
