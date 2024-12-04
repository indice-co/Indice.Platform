import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClaimTypeInfo, ApiScopeInfo } from 'src/app/core/services/identity-api.service';
import { ApiResourceStore } from '../../../api-resource-store.service';

@Component({
    selector: 'app-api-resource-scope-claims',
    templateUrl: './api-resource-scope-claims.component.html'
})
export class ApiResourceScopeClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addApiResourceScopeClaim: Subscription;
    private _deleteApiResourceScopeClaim: Subscription;
    private _apiResourceId: number;

    constructor(
        private _route: ActivatedRoute,
        private _apiResourceStore: ApiResourceStore,
        private _authService: AuthService
    ) { }

    @Input() public scope = new ApiScopeInfo();
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
        this._apiResourceId = +this._route.parent.snapshot.params['id'];
        this._apiResourceStore.getAllClaims().subscribe((allClaims: ClaimTypeInfo[]) => {
            const scopeClaims = this.scope.userClaims || [];
            this.availableClaims = allClaims.filter(x => !scopeClaims.includes(x.name));
            this.selectedClaims = allClaims.filter(x => scopeClaims.includes(x.name));
            this.count = this.selectedClaims.length;
            this.rows = this.selectedClaims;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addApiResourceScopeClaim) {
            this._addApiResourceScopeClaim.unsubscribe();
        }
        if (this._deleteApiResourceScopeClaim) {
            this._deleteApiResourceScopeClaim.unsubscribe();
        }
    }

    public addClaim(claim: ClaimTypeInfo): void {
        this._addApiResourceScopeClaim = this._apiResourceStore.addApiResourceScopeClaim(this._apiResourceId, this.scope.id, claim).subscribe();
    }

    public removeClaim(claim: ClaimTypeInfo): void {
        this._deleteApiResourceScopeClaim = this._apiResourceStore.deleteApiResourceScopeClaim(this._apiResourceId, this.scope.id, claim).subscribe();
    }
}
