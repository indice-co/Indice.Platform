import { Component, OnInit, OnDestroy } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { Subscription } from 'rxjs';
import { ClaimTypeInfo, IdentityApiService, ClaimTypeInfoResultSet } from 'src/app/core/services/identity-api.service';
import { StepBaseComponent } from 'src/app/shared/components/step-base/step-base.component';
import { ResourceWizardModel } from '../../../models/resource-wizard-model';

@Component({
    selector: 'app-user-claims-step',
    templateUrl: './user-claims-step.component.html'
})
export class UserClaimsStepComponent extends StepBaseComponent<ResourceWizardModel> implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addApiResourceScopeClaim: Subscription;
    private _selectedClaimsControl: AbstractControl;

    constructor(private _api: IdentityApiService) {
        super();
    }

    public availableClaims: ClaimTypeInfo[];
    public selectedClaims: ClaimTypeInfo[];

    public ngOnInit(): void {
        this._api.getClaimTypes(1, 2147483647, 'name+', undefined).subscribe((response: ClaimTypeInfoResultSet) => {
            this._selectedClaimsControl = this.data.form.controls['userClaims'];
            this.availableClaims = response.items.filter(x => !this._selectedClaimsControl.value.includes(x.name));
            this.selectedClaims = response.items.filter(x => this._selectedClaimsControl.value.includes(x.name));
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addApiResourceScopeClaim) {
            this._addApiResourceScopeClaim.unsubscribe();
        }
    }

    public addClaim(claim: ClaimTypeInfo): void {
        const resources = this._selectedClaimsControl.value as Array<string>;
        resources.push(claim.name);
        this._selectedClaimsControl.setValue(resources);
    }

    public removeClaim(claim: ClaimTypeInfo): void {
        const resources = this._selectedClaimsControl.value as Array<string>;
        const index = resources.indexOf(claim.name, 0);
        if (index > -1) {
            resources.splice(index, 1);
        }
        this._selectedClaimsControl.setValue(resources);
    }

    public isValid(): boolean {
        return true;
    }
}
