import { Component, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { IdentityApiService, HttpValidationProblemDetails, ProblemDetails, RoleInfo, CreateRoleRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ValidationSummaryComponent } from 'src/app/shared/components/validation-summary/validation-summary.component';

@Component({
    selector: 'app-role-add',
    templateUrl: './role-add.component.html'
})
export class RoleAddComponent {
    @ViewChild('validationSummary', { static: false }) private _validationSummary: ValidationSummaryComponent;

    constructor(private _api: IdentityApiService, private _router: Router, private _route: ActivatedRoute, public _toast: ToastService) { }

    public role: CreateRoleRequest = new CreateRoleRequest();
    public problemDetails: ProblemDetails;

    public save(): void {
        this._validationSummary.clear();
        this._api.createRole(this.role).subscribe((response: RoleInfo) => {
            this._toast.showSuccess(`Role '${response.name}' was created successfully.`);
            this._router.navigate(['../'], { relativeTo: this._route });
        }, (problemDetails: HttpValidationProblemDetails) => {
            this.problemDetails = problemDetails;
        });
    }
}
