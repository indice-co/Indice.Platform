import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { ApiResourceInfo, ScopeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../api-resource-store.service';

@Component({
    selector: 'app-api-resource-scopes',
    templateUrl: './api-resource-scopes.component.html'
})
export class ApiResourceScopesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore, public _toast: ToastService, private _router: Router) { }

    public apiResource: ApiResourceInfo;
    public detailsActive = true;

    public ngOnInit(): void {
        const apiResourceId = +this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._apiResourceStore.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            this.apiResource = apiResource;
            apiResource.scopes.forEach((value: ScopeInfo, index: number) => {
                (value as any).isOpen = index === 0 ? true : false;
            });
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public openPanel(itemIndex: number): void {
        this.apiResource.scopes.forEach((value: ScopeInfo, index: number) => {
            (value as any).isOpen = itemIndex === index;
        });
    }
}
