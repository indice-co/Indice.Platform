import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { ApiResourceStore } from './api-resource-store.service';
import { ApiResourceInfo } from 'src/app/core/services/identity-api.service';

@Component({
    selector: 'app-api-resource-edit',
    templateUrl: './api-resource-edit.component.html',
    providers: [ApiResourceStore]
})
export class ApiResourceEditComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore) { }

    public apiResourceName = '';

    public ngOnInit(): void {
        const apiResourceId = +this._route.snapshot.params.id;
        this._getDataSubscription = this._apiResourceStore.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            this.apiResourceName = apiResource.displayName || apiResource.name;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }
}
