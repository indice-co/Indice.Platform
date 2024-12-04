import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from 'src/app/core/services/auth.service';

import { UpdateClaimTypeRequest, ClaimValueType, IdentityApiService, ClaimTypeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-claim-type-edit',
    templateUrl: './claim-type-edit.component.html'
})
export class ClaimTypeEditComponent implements OnInit {
    constructor(
        private _api: IdentityApiService,
        private _router: Router,
        private _route: ActivatedRoute,
        public _toast: ToastService,
        private _authService: AuthService
    ) { }

    public claimType: ClaimTypeInfo = new ClaimTypeInfo();
    public valueTypes: string[] = [];
    public claimValueType = '';
    public canEditClaimType: boolean;

    public ngOnInit(): void {
        this.canEditClaimType = this._authService.isAdminUIUsersWriter() || this._authService.isAdminUIClientsWriter();
        this.claimType = this._route.snapshot.data['claimType'];
        this.claimValueType = this.claimType.valueType;
        for (const type in ClaimValueType) {
            if (type) {
                this.valueTypes.push(type);
            }
        }
    }

    public delete(): void {
        this._api.deleteClaimType(this.claimType.id).subscribe(_ => {
            this._toast.showSuccess(`Role '${this.claimType.name}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        this.claimType.valueType = this.claimValueType as ClaimValueType;
        this._api.updateClaimType(this.claimType.id, {
            description: this.claimType.description,
            required: this.claimType.required,
            rule: this.claimType.rule,
            userEditable: this.claimType.userEditable,
            valueType: this.claimValueType,
            displayName: this.claimType.displayName
        } as UpdateClaimTypeRequest).subscribe((response: ClaimTypeInfo) => {
            this._toast.showSuccess(`Claim type '${response.name}' was updated successfully.`);
        });
    }
}
