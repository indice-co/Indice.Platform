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
                apiResource.scopes = apiResource.scopes.sort((left: ScopeInfo, right: ScopeInfo) => (left.name > right.name ? 1 : -1));
                apiResource.scopes.forEach((value: ScopeInfo) => {
                    (value as any).isOpen = false;
                    value.userClaims = value.userClaims || [];
                });
                this._apiResource.next(apiResource);
                this._apiResource.complete();
            });
        }
        return this._apiResource;
    }

    public addApiResourceClaim(apiResourceId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            apiResource.allowedClaims.push(claim.name);
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.addProtectedResourceClaims(apiResourceId, [claim.name]);
    }

    public deleteApiResourceClaim(apiResourceId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const index = apiResource.allowedClaims.indexOf(claim.name);
            if (index > -1) {
                apiResource.allowedClaims.splice(index, 1);
            }
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.deleteProtectedResourceClaim(apiResourceId, claim.name);
    }

    public addApiResourceScopeClaim(apiResourceId: number, scopeId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const scopeClaims = apiResource.scopes.find(x => x.id === scopeId).userClaims;
            scopeClaims.push(claim.name);
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.addProtectedResourceScopeClaims(apiResourceId, scopeId, [claim.name]);
    }

    public deleteApiResourceScopeClaim(apiResourceId: number, scopeId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const scope = apiResource.scopes.find(x => x.id === scopeId);
            const index = scope.userClaims.indexOf(claim.name);
            if (index > -1) {
                scope.userClaims.splice(index, 1);
            }
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.deleteProtectedResourceScopeClaim(apiResourceId, scopeId, claim.name);
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
