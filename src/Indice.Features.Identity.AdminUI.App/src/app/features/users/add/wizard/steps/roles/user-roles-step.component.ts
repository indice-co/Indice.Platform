import { Component, OnInit } from "@angular/core";
import { StepBaseComponent } from "src/app/shared/components/step-base/step-base.component";
import { UserWizardModel } from "../../models/user-wizard.model";
import { Subscription } from "rxjs";
import { AbstractControl } from "@angular/forms";
import {
  IdentityApiService,
  RoleInfo,
  RoleInfoResultSet,
} from "src/app/core/services/identity-api.service";

@Component({
  selector: "app-roles-step",
  templateUrl: "./user-roles-step.component.html",
})
export class UserRolesStepComponent
  extends StepBaseComponent<UserWizardModel>
  implements OnInit
{
  private _getDataSubscription: Subscription;
  private _addApiResourceScopeRoles: Subscription;
  private _selectedRolesControl: AbstractControl;

  constructor(private _api: IdentityApiService) {
    super();
  }

  public availableRoles: RoleInfo[];
  public selectedRoles: RoleInfo[];

  public ngOnInit(): void {
    this._api
      .getRoles(1, 2147483647, "name+", undefined)
      .subscribe((response: RoleInfoResultSet) => {
        this._selectedRolesControl = this.data.form.controls["userRoles"];
        this.availableRoles = response.items.filter(
          (x) => !this._selectedRolesControl.value.includes(x.name)
        );
        this.selectedRoles = response.items.filter((x) =>
          this._selectedRolesControl.value.includes(x.name)
        );
      });
  }

  public ngOnDestroy(): void {
    if (this._getDataSubscription) {
      this._getDataSubscription.unsubscribe();
    }
    if (this._addApiResourceScopeRoles) {
      this._addApiResourceScopeRoles.unsubscribe();
    }
  }

  public addRole(claim: RoleInfo): void {
    const resources = this._selectedRolesControl.value as Array<string>;
    resources.push(claim.name);
    this._selectedRolesControl.setValue(resources);
  }

  public removeRole(claim: RoleInfo): void {
    const resources = this._selectedRolesControl.value as Array<string>;
    const index = resources.indexOf(claim.name, 0);
    if (index > -1) {
      resources.splice(index, 1);
    }
    this._selectedRolesControl.setValue(resources);
  }

  public isValid(): boolean {
    return true;
  }
}
