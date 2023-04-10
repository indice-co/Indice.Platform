import { Component, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { IdentityApiService, HttpValidationProblemDetails, ProblemDetails, CreateAppSettingRequest, AppSettingInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ValidationSummaryComponent } from 'src/app/shared/components/validation-summary/validation-summary.component';

@Component({
    selector: 'app-setting-add',
    templateUrl: './setting-add.component.html'
})
export class SettingAddComponent {
    @ViewChild('validationSummary', { static: false }) private _validationSummary: ValidationSummaryComponent;

    constructor(private _api: IdentityApiService, private _router: Router, private _route: ActivatedRoute, public _toast: ToastService) { }

    public setting: CreateAppSettingRequest = new CreateAppSettingRequest();
    public problemDetails: ProblemDetails;

    public save(): void {
        this._validationSummary.clear();
        this._api.createSetting(this.setting).subscribe((response: AppSettingInfo) => {
            this._toast.showSuccess(`Setting '${response.key}' was created successfully.`);
            this._router.navigate(['../'], { relativeTo: this._route });
        }, (problemDetails: HttpValidationProblemDetails) => {
            this.problemDetails = problemDetails;
        });
    }
}
