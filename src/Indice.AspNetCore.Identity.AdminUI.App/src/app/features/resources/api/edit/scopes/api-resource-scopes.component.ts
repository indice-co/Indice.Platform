import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { ApiResourceInfo, ApiScopeInfo } from 'src/app/core/services/identity-api.service';
import { ApiResourceStore } from '../../api-resource-store.service';

@Component({
    selector: 'app-api-resource-scopes',
    templateUrl: './api-resource-scopes.component.html'
})
export class ApiResourceScopesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute, 
        private _apiResourceStore: ApiResourceStore,
        private _authService: AuthService
    ) { }

    public apiResource: ApiResourceInfo;
    public detailsActive = true;
    public canEditResource: boolean;

    public ngOnInit(): void {
        this.canEditResource = this._authService.isAdminUIClientsWriter();
        const apiResourceId = +this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._apiResourceStore.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            this.apiResource = apiResource;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public togglePanel(itemIndex: number): void {
        this.apiResource.scopes.forEach((value: ApiScopeInfo, index: number) => {
            const isOpen = (value as any).isOpen;
            (value as any).isOpen = itemIndex === index ? !isOpen : false;
        });
    }
}
