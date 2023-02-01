import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { ApiResourceInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../../api-resource-store.service';

@Component({
    selector: 'app-api-resource-details',
    templateUrl: './api-resource-details.component.html'
})
export class ApiResourceDetailsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _updateApiResourceSubscription: Subscription;
    private _deleteApiResourceSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute,
        private _apiResourceStore: ApiResourceStore,
        public _toast: ToastService,
        private _router: Router,
        private _authService: AuthService
    ) { }

    public apiResource: ApiResourceInfo;
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
        if (this._updateApiResourceSubscription) {
            this._updateApiResourceSubscription.unsubscribe();
        }
        if (this._deleteApiResourceSubscription) {
            this._deleteApiResourceSubscription.unsubscribe();
        }
    }

    public delete(): void {
        this._deleteApiResourceSubscription = this._apiResourceStore.deleteApiResource(this.apiResource.id).subscribe(_ => {
            this._toast.showSuccess(`API resource '${this.apiResource.name}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        this._updateApiResourceSubscription = this._apiResourceStore.updateApiResource(this.apiResource).subscribe(_ => {
            this._toast.showSuccess(`API resource '${this.apiResource.name}' was updated successfully.`);
        });
    }
}
