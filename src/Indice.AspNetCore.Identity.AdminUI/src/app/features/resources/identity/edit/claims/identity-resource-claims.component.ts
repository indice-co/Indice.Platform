import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { ClaimTypeInfo, IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { IdentityResourceStore } from '../identity-resource-store.service';

@Component({
    selector: 'app-identity-resource-claims',
    templateUrl: './identity-resource-claims.component.html'
})
export class IdentityResourceClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _identityResourceId: number;

    constructor(private _route: ActivatedRoute, private _identityResourceStore: IdentityResourceStore) { }

    public availableClaims: ClaimTypeInfo[];
    public identityResourceClaims: ClaimTypeInfo[];

    public ngOnInit(): void {
        this._identityResourceId = this._route.parent.snapshot.params.id;
        const getIdentityResource = this._identityResourceStore.getIdentityResource(this._identityResourceId);
        const getAllClaims = this._identityResourceStore.getAllClaims();
        this._getDataSubscription = forkJoin([getIdentityResource, getAllClaims]).pipe(map((responses: [IdentityResourceInfo, ClaimTypeInfo[]]) => {
            return {
                resource: responses[0],
                claimTypes: responses[1]
            };
        })).subscribe((result: { resource: IdentityResourceInfo, claimTypes: ClaimTypeInfo[] }) => {
            const identityResourceClaims = result.resource.allowedClaims;
            const allClaimTypes = result.claimTypes;
            this.availableClaims = allClaimTypes.filter(x => !identityResourceClaims.includes(x.name));
            this.identityResourceClaims = allClaimTypes.filter(x => identityResourceClaims.includes(x.name));
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public addClaim(claim: ClaimTypeInfo): void { }

    public removeClaim(claim: ClaimTypeInfo): void { }
}
