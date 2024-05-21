import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClaimTypeInfo, IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { IdentityResourceStore } from '../identity-resource-store.service';

@Component({
    selector: 'app-identity-resource-claims',
    templateUrl: './identity-resource-claims.component.html'
})
export class IdentityResourceClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addIdentityResourceClaim: Subscription;
    private _deleteIdentityResourceClaim: Subscription;
    private _identityResourceId: number;

    constructor(
        private _route: ActivatedRoute,
        private _identityResourceStore: IdentityResourceStore,
        private _authService: AuthService
    ) { }

    public availableClaims: ClaimTypeInfo[];
    public identityResourceClaims: ClaimTypeInfo[];
    public canEditResource: boolean;
    public rows: ClaimTypeInfo[] = [];
    public columns: TableColumn[] = [];
    public count = 0;

    public ngOnInit(): void {
        this.canEditResource = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: true },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: true }
        ];
        this._identityResourceId = this._route.parent.snapshot.params['id'];
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
            this.count = this.identityResourceClaims.length;
            this.rows = this.identityResourceClaims;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addIdentityResourceClaim) {
            this._addIdentityResourceClaim.unsubscribe();
        }
        if (this._deleteIdentityResourceClaim) {
            this._deleteIdentityResourceClaim.unsubscribe();
        }
    }

    public addClaim(claim: ClaimTypeInfo): void {
        this._addIdentityResourceClaim = this._identityResourceStore.addIdentityResourceClaim(this._identityResourceId, claim).subscribe();
    }

    public removeClaim(claim: ClaimTypeInfo): void {
        this._deleteIdentityResourceClaim = this._identityResourceStore.deleteIdentityResourceClaim(this._identityResourceId, claim).subscribe();
    }
}
