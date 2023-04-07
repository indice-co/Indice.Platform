import { Component, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { CreateUserRequest, IdentityApiService, SingleUserInfo, ProblemDetails, HttpValidationProblemDetails } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ValidationSummaryComponent } from 'src/app/shared/components/validation-summary/validation-summary.component';

@Component({
  selector: 'app-user-add',
  templateUrl: './user-add.component.html'
})
export class UserAddComponent {
  @ViewChild('validationSummary', { static: false }) private _validationSummary: ValidationSummaryComponent;
  private _saveAndConfigure = false;

  constructor(private _api: IdentityApiService, private _route: ActivatedRoute, private _router: Router, public _toast: ToastService) { }

  public user: CreateUserRequest = new CreateUserRequest();
  public problemDetails: ProblemDetails;

  public setSaveAndConfigure(value: boolean) {
    this._saveAndConfigure = value;
  }

  public save(): void {
    this._validationSummary.clear();
    this._api.createUser(this.user).subscribe((createdUser: SingleUserInfo) => {
      this._toast.showSuccess(`User '${createdUser.email}' was created successfully.`);
      if (this._saveAndConfigure) {
        this._router.navigateByUrl(`/app/users/${createdUser.id}/details`);
      } else {
        this._router.navigate(['../'], { relativeTo: this._route });
      }
    }, (problemDetails: HttpValidationProblemDetails) => {
      this.problemDetails = problemDetails;
    });
  }
}
