import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { ClaimTypeInfo, ScopeInfo } from 'src/app/core/services/identity-api.service';
import { ApiResourceStore } from '../../api-resource-store.service';

@Component({
    selector: 'app-api-resource-scope-claims',
    templateUrl: './api-resource-scope-claims.component.html'
})
export class ApiResourceScopeClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore) { }

    @Input() public scope = new ScopeInfo();
    public availableClaims: ClaimTypeInfo[];
    public selectedClaims: ClaimTypeInfo[];

    public ngOnInit(): void {
        this._apiResourceStore.getAllClaims().subscribe((allClaims: ClaimTypeInfo[]) => {
            const scopeClaims = this.scope.userClaims || [];
            this.availableClaims = allClaims.filter(x => !scopeClaims.includes(x.name));
            this.selectedClaims = allClaims.filter(x => scopeClaims.includes(x.name));
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
