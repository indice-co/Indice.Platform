import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { IdentityResourceStore } from '../identity-resource-store.service';
import { ClaimType } from 'src/app/features/users/edit/details/models/claim-type.model';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-identity-resource-details',
    templateUrl: './identity-resource-details.component.html'
})
export class IdentityResourceDetailsComponent implements OnInit, OnDestroy {
    private _updateIdentityResourceSubscription: Subscription;
    private _deleteIdentityResourceSubscription: Subscription;
    private _getResourceSubscription: Subscription;

    constructor(private _router: Router, private _route: ActivatedRoute, private _identityResourceStore: IdentityResourceStore, public _toast: ToastService) { }

    public identityResource: IdentityResourceInfo;
    public requiredClaims: ClaimType[];

    public ngOnInit(): void {
        const resourceId = +this._route.parent.snapshot.params.id;
        this._getResourceSubscription = this._identityResourceStore.getIdentityResource(resourceId).subscribe((resource: IdentityResourceInfo) => {
            this.identityResource = resource;
        });
    }

    public ngOnDestroy(): void {
        if (this._getResourceSubscription) {
            this._getResourceSubscription.unsubscribe();
        }
        if (this._updateIdentityResourceSubscription) {
            this._updateIdentityResourceSubscription.unsubscribe();
        }
        if (this._deleteIdentityResourceSubscription) {
            this._deleteIdentityResourceSubscription.unsubscribe();
        }
    }

    public delete(): void {
        this._deleteIdentityResourceSubscription = this._identityResourceStore.deleteIdentityResource(this.identityResource.id).subscribe(_ => {
            this._toast.showSuccess(`Identity resource '${this.identityResource.name}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        this._updateIdentityResourceSubscription = this._identityResourceStore.updateIdentityResource(this.identityResource).subscribe(_ => {
            this._toast.showSuccess(`Identity resource '${this.identityResource.name}' was updated successfully.`);
        });
    }
}
