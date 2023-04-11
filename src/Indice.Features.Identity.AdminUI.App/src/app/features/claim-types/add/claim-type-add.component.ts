import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { CreateClaimTypeRequest, ClaimValueType, IdentityApiService, ClaimTypeInfo, HttpValidationProblemDetails, ProblemDetails } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ValidationSummaryComponent } from 'src/app/shared/components/validation-summary/validation-summary.component';

@Component({
    selector: 'app-claim-type-add',
    templateUrl: './claim-type-add.component.html'
})
export class ClaimTypeAddComponent implements OnInit {
    @ViewChild('validationSummary', { static: false }) private _validationSummary: ValidationSummaryComponent;

    constructor(private _api: IdentityApiService, private _router: Router, private _route: ActivatedRoute, public _toast: ToastService) { }

    public claimType: CreateClaimTypeRequest = new CreateClaimTypeRequest();
    public valueTypes: string[] = [];
    public errors: string[] = [];
    public problemDetails: ProblemDetails;
    public claimValueType = '';

    public ngOnInit(): void {
        for (const type in ClaimValueType) {
            if (type) {
                this.valueTypes.push(type);
            }
        }
    }

    public save(): void {
        this._validationSummary.clear();
        this.claimType.valueType = this.claimValueType as ClaimValueType;
        this._api.createClaimType(this.claimType).subscribe((response: ClaimTypeInfo) => {
            this._toast.showSuccess(`Claim type '${response.name}' was created successfully.`);
            this._router.navigate(['../'], { relativeTo: this._route });
        }, (problemDetails: HttpValidationProblemDetails) => {
            this.problemDetails = problemDetails;
        });
    }
}
