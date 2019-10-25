import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { ApiResourceInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../../api-resource-store.service';

@Component({
    selector: 'app-api-resource-details',
    templateUrl: './api-resource-details.component.html'
})
export class ApiResourceDetailsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore, public _toast: ToastService, private _router: Router) { }

    public apiResource: ApiResourceInfo;

    public ngOnInit(): void {
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

    public delete(): void {
        // this._apiResourceStore.deleteClient(this.client.clientId).subscribe(_ => {
        //     this._toast.showSuccess(`Client '${this.client.clientName}' was deleted successfully.`);
        //     this._router.navigate(['../../'], { relativeTo: this._route });
        // });
    }

    public update(): void {
        this._apiResourceStore.updateApiResource(this.apiResource).subscribe(_ => {
            this._toast.showSuccess(`API resource '${this.apiResource.name}' was updated successfully.`);
        });
    }
}
