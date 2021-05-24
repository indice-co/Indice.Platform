import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription, forkJoin } from 'rxjs';
import { ClaimTypeInfo, ApiResourceInfo } from 'src/app/core/services/identity-api.service';
import { ApiResourceStore } from '../../api-resource-store.service';
import { map } from 'rxjs/operators';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-api-resource-claims',
    templateUrl: './api-resource-claims.component.html'
})
export class ApiResourceClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addApiResourceClaim: Subscription;
    private _deleteApiResourceClaim: Subscription;
    private _apiResourceId: number;

    constructor(
        private _route: ActivatedRoute,
        private _apiResourceStore: ApiResourceStore,
        private _authService: AuthService
    ) { }

    public availableClaims: ClaimTypeInfo[];
    public selectedClaims: ClaimTypeInfo[];
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
        this._apiResourceId = +this._route.parent.snapshot.params.id;
        const getApiResource = this._apiResourceStore.getApiResource(this._apiResourceId);
        const getAllClaims = this._apiResourceStore.getAllClaims();
        this._getDataSubscription = forkJoin([getApiResource, getAllClaims]).pipe(map((responses: [ApiResourceInfo, ClaimTypeInfo[]]) => {
            return {
                apiResource: responses[0],
                allClaims: responses[1]
            };
        })).subscribe((result: { apiResource: ApiResourceInfo, allClaims: ClaimTypeInfo[] }) => {
            const resourceClaims = result.apiResource.allowedClaims;
            const allClaims = result.allClaims;
            this.availableClaims = allClaims.filter(x => !resourceClaims.includes(x.name));
            this.selectedClaims = allClaims.filter(x => resourceClaims.includes(x.name));
            this.count = this.selectedClaims.length;
            this.rows = this.selectedClaims;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addApiResourceClaim) {
            this._addApiResourceClaim.unsubscribe();
        }
        if (this._deleteApiResourceClaim) {
            this._deleteApiResourceClaim.unsubscribe();
        }
    }

    public addClaim(claim: ClaimTypeInfo): void {
        this._addApiResourceClaim = this._apiResourceStore.addApiResourceClaim(this._apiResourceId, claim).subscribe();
    }

    public removeClaim(claim: ClaimTypeInfo): void {
        this._deleteApiResourceClaim = this._apiResourceStore.deleteApiResourceClaim(this._apiResourceId, claim).subscribe();
    }
}
