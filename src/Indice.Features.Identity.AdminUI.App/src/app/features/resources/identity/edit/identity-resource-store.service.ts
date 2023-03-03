import { Injectable } from '@angular/core';

import { AsyncSubject, Observable, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { IdentityApiService, IdentityResourceInfo, ClaimTypeInfo, ClaimTypeInfoResultSet, UpdateIdentityResourceRequest, IUpdateIdentityResourceRequest } from 'src/app/core/services/identity-api.service';

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

    public updateIdentityResource(identityResource: IdentityResourceInfo): Observable<void> {
        return this._api.updateIdentityResource(identityResource.id, new UpdateIdentityResourceRequest({
            displayName: identityResource.displayName,
            description: identityResource.description,
            enabled: identityResource.enabled,
            emphasize: identityResource.emphasize,
            required: identityResource.required,
            showInDiscoveryDocument: identityResource.showInDiscoveryDocument
        } as IUpdateIdentityResourceRequest)).pipe(map(_ => {
            this._identityResource.next(identityResource);
            this._identityResource.complete();
        }));
    }

    public deleteIdentityResource(resourceId: number): Observable<void> {
        return this._api.deleteIdentityResource(resourceId);
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

    public addIdentityResourceClaim(resourceId: number, claim: ClaimTypeInfo): Observable<void> {
        const getIdentityResource = this.getIdentityResource(resourceId);
        const addClaim = this._api.addIdentityResourceClaims(resourceId, [claim.name]);
        return forkJoin([getIdentityResource, addClaim]).pipe(map((responses: [IdentityResourceInfo, void]) => {
            return {
                identityResource: responses[0]
            };
        })).pipe(map((result: { identityResource: IdentityResourceInfo }) => {
            result.identityResource.allowedClaims.push(claim.name);
            this._identityResource.next(result.identityResource);
            this._identityResource.complete();
        }));
    }

    public deleteIdentityResourceClaim(resourceId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getIdentityResource(resourceId).subscribe((identityResource: IdentityResourceInfo) => {
            const index = identityResource.allowedClaims.indexOf(claim.name);
            if (index > -1) {
                identityResource.allowedClaims.splice(index, 1);
            }
            this._identityResource.next(identityResource);
            this._identityResource.complete();
        });
        return this._api.deleteIdentityResourceClaim(resourceId, claim.name);
    }
}
