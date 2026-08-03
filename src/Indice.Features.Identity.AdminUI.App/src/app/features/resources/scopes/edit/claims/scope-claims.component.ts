import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, ClaimTypeInfo, ClaimTypeInfoResultSet } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-scope-claims',
    templateUrl: './scope-claims.component.html',
    standalone: false
})
export class ScopeClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addScopeClaim: Subscription;
    private _deleteScopeClaim: Subscription;
    private _scopeName: string;

    constructor(
        private _route: ActivatedRoute,
        private _api: IdentityApiService,
        private _authService: AuthService,
        private _toast: ToastService
    ) { }

    public availableClaims: ClaimTypeInfo[] = [];
    public scopeClaims: ClaimTypeInfo[] = [];
    public canEditScope: boolean;
    public rows: ClaimTypeInfo[] = [];
    public columns: TableColumn[] = [];
    public count = 0;

    public ngOnInit(): void {
        this.canEditScope = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: true },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: true }
        ];
        this._scopeName = this._route.parent?.snapshot.params['name'];
        this.loadData();
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addScopeClaim) {
            this._addScopeClaim.unsubscribe();
        }
        if (this._deleteScopeClaim) {
            this._deleteScopeClaim.unsubscribe();
        }
    }

    public addClaim(claim: ClaimTypeInfo): void {
        this._addScopeClaim = this._api.addApiScopeClaims(this._scopeName, [claim.name!]).subscribe(() => {
            this._toast.showSuccess(`Claim '${claim.name}' was added successfully.`);
            this.loadData();
        });
    }

    public removeClaim(claim: ClaimTypeInfo): void {
        this._deleteScopeClaim = this._api.deleteApiScopeClaim(this._scopeName, claim.name!).subscribe(() => {
            this._toast.showSuccess(`Claim '${claim.name}' was removed successfully.`);
            this.loadData();
        });
    }

    private loadData(): void {
        const getScope = this._api.getApiScopes(1, 1000);
        const getAllClaims = this._api.getClaimTypes(1, 1000);

        this._getDataSubscription = forkJoin([getScope, getAllClaims]).pipe(map((responses) => {
            return {
                scope: responses[0].items?.find(s => s.name === this._scopeName),
                claimTypes: responses[1].items || []
            };
        })).subscribe((result) => {
            const scopeUserClaims = result.scope?.userClaims || [];
            const allClaimTypes = result.claimTypes;
            this.availableClaims = allClaimTypes.filter(x => !scopeUserClaims.includes(x.name!));
            this.scopeClaims = allClaimTypes.filter(x => scopeUserClaims.includes(x.name!));
            this.count = this.scopeClaims.length;
            this.rows = this.scopeClaims;
        });
    }
}
