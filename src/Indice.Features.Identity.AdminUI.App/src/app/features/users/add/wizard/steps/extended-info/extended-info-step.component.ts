import { Component, OnInit } from "@angular/core";
import { StepBaseComponent } from "src/app/shared/components/step-base/step-base.component";
import { UserWizardModel } from "../../models/user-wizard.model";
import { AbstractControl } from "@angular/forms";

@Component({
  selector: "app-extended-info-step",
  templateUrl: "./extended-info-step.component.html",
})
export class ExtendedInfoStepComponent
  extends StepBaseComponent<UserWizardModel>
  implements OnInit
{
  constructor() {
    super();
  }

  public get firstName(): AbstractControl {
    return this.data.form.get("firstName");
  }

  public get lastName(): AbstractControl {
    return this.data.form.get("lastName");
  }

  public get userName(): AbstractControl {
    return this.data.form.get("userName");
  }

  public get emailAsUserName(): AbstractControl {
    return this.data.form.get("emailAsUserName");
  }

  public get email(): AbstractControl {
    return this.data.form.get("email");
  }

  public get emailConfirmed(): AbstractControl {
    return this.data.form.get("emailConfirmed");
  }

  public get phoneNumber(): AbstractControl {
    return this.data.form.get("phoneNumber");
  }

  public get phoneNumberConfirmed(): AbstractControl {
    return this.data.form.get("phoneNumberConfirmed");
  }

  public get password(): AbstractControl {
    return this.data.form.get("password");
  }

  public get bypassPasswordValidation(): AbstractControl {
    return this.data.form.get("bypassPasswordValidation");
  }

  public ngOnInit(): void {}

  public toggleEmailAsUserName({ target }: Event): void {
    const { checked } = target as HTMLInputElement;
    if (checked) {
      this.userName.patchValue(this.email.value);
    }
  }

  public checkUserNameShouldUpdate(): void {
    const shouldUpdate =
      this.emailAsUserName.value && this.email.value !== this.userName.value;
    if (shouldUpdate) {
      this.userName.patchValue(this.email.value);
    }
  }

  public isValid(): boolean {
    return this.data.form.valid;
  }
}
