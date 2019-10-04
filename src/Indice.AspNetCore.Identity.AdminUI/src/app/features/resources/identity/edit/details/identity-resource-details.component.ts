import { Component, ViewChild, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgForm } from '@angular/forms';

import { Subscription } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { IdentityResourceStore } from '../identity-resource-store.service';
import { ClaimType } from 'src/app/features/users/edit/details/models/claim-type.model';

@Component({
    selector: 'app-identity-resource-details',
    templateUrl: './identity-resource-details.component.html'
})
export class IdentityResourceDetailsComponent implements OnInit, OnDestroy {
    private _updateIdentityResourceSubscription: Subscription;
    private _getResourceSubscription: Subscription;
    @ViewChild('form', { static: false }) private _form: NgForm;
    @ViewChild('updateSuccessAlert', { static: false }) private _updateSuccessAlert: SwalComponent;
    @ViewChild('deleteSuccessAlert', { static: false }) private _deleteSuccessAlert: SwalComponent;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;

    constructor(private _router: Router, private _route: ActivatedRoute, private _identityResourceStore: IdentityResourceStore) { }

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
    }

    public deletePrompt(): void {
        this._deleteAlert.fire();
    }

    public delete(): void {
        this._deleteSuccessAlert.fire();
    }

    public goToList(): void {
        this._router.navigateByUrl('/app/resources/identity');
    }

    public update(): void {
        const requiredClaims = this.requiredClaims.map(x => Object.assign({}, x));
        // requiredClaims.forEach((claim: ClaimType) => {
        //     if (claim.valueType === ValueType.DateTime) {
        //         const date = claim.value as NgbDateStruct;
        //         claim.value = this.dateParser.format(date);
        //     }
        // });
        // this._updateIdentityResourceSubscription = this._identityResourceStore.updateUser(this.identityResource, requiredClaims).subscribe(_ => {
        //     this._updateSuccessAlert.fire();
        // });
    }
}
