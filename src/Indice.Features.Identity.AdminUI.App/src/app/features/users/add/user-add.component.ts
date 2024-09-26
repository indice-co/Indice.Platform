import {
  ChangeDetectorRef,
  Component,
  ComponentFactoryResolver,
  OnInit,
  ViewChild,
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";

import {
  CreateUserRequest,
  IdentityApiService,
  SingleUserInfo,
  ProblemDetails,
  HttpValidationProblemDetails,
} from "src/app/core/services/identity-api.service";
import { ToastService } from "src/app/layout/services/app-toast.service";
import { StepBaseComponent } from "src/app/shared/components/step-base/step-base.component";
import { WizardStepDirective } from "src/app/shared/components/step-base/wizard-step.directive";
import { ValidationSummaryComponent } from "src/app/shared/components/validation-summary/validation-summary.component";
import { UserWizardModel } from "./wizard/models/user-wizard.model";
import { WizardStepDescriptor } from "src/app/shared/components/step-base/models/wizard-step-descriptor";
import {
  UntypedFormBuilder,
  UntypedFormControl,
  UntypedFormGroup,
  Validators,
} from "@angular/forms";
import { UserClaimsStepComponent } from "./wizard/steps/claims/user-claims-step.component";
import { ExtendedInfoStepComponent } from "./wizard/steps/extended-info/extended-info-step.component";
import { UserRolesStepComponent } from "./wizard/steps/roles/user-roles-step.component";
import { Subscription } from "rxjs";

@Component({
  selector: "app-user-add",
  templateUrl: "./user-add.component.html",
  providers: [],
})
export class UserAddComponent implements OnInit {
  @ViewChild("validationSummary", { static: false })
  private _validationSummary: ValidationSummaryComponent;
  @ViewChild(WizardStepDirective, { static: false })
  private _wizardStepHost: WizardStepDirective;
  private _loadedStepInstance: StepBaseComponent<UserWizardModel>;
  private _formValidatedSubscription: Subscription;
  private _navigationOrigin: string;
  private _saveAndConfigure = false;

  public wizardStepIndex = 0;
  public apiResourceSteps: WizardStepDescriptor[] = [];
  public form: UntypedFormGroup;
  public hostFormValidated = false;
  public resource: CreateUserRequest = new CreateUserRequest();

  public get canGoFront(): boolean {
    return (
      this.wizardStepIndex >= 0 &&
      this.wizardStepIndex < this.apiResourceSteps.length - 1
    );
  }

  public get canGoBack(): boolean {
    return (
      this.wizardStepIndex > 0 &&
      this.wizardStepIndex <= this.apiResourceSteps.length - 1
    );
  }

  public get isSummaryStep(): boolean {
    return this.wizardStepIndex === this.apiResourceSteps.length - 1;
  }

  constructor(
    private _componentFactoryResolver: ComponentFactoryResolver,
    private _changeDetectionRef: ChangeDetectorRef,
    private _formBuilder: UntypedFormBuilder,
    private _api: IdentityApiService,
    private _route: ActivatedRoute,
    private _router: Router,
    public _toast: ToastService
  ) {}

  public ngOnInit(): void {
    this.form = this._formBuilder.group({
      firstName: ["", [Validators.required, Validators.maxLength(200)]],
      lastName: ["", [Validators.required, Validators.maxLength(200)]],
      userName: ["", [Validators.maxLength(1000)]],
      email: ["", [Validators.required, Validators.email]],
      emailConfirmed: [false],
      phoneNumber: [""],
      phoneNumberConfirmed: [false],
      password: ["", [Validators.required]],
      bypassPasswordValidation: [false],
      userClaims: [[]],
      userRoles: [[]],
    });
    this.apiResourceSteps = [
      new WizardStepDescriptor("Extended Details", ExtendedInfoStepComponent),
      new WizardStepDescriptor("User Claims", UserClaimsStepComponent),
      new WizardStepDescriptor("User Roles", UserRolesStepComponent),
    ];
    this._navigationOrigin = history.state.origin;
    this._changeDetectionRef.detectChanges();
    this.loadStep(this.apiResourceSteps[0]);
  }

  public goToNextStep(): void {
    if (!this.canGoFront) {
      return;
    }
    if (this._loadedStepInstance.isValid()) {
      this.hostFormValidated = false;
      this.wizardStepIndex += 1;
      this.loadStep(this.apiResourceSteps[this.wizardStepIndex]);
    } else {
      this.hostFormValidated = true;
      this.validateFormFields(this.form);
    }
    this._loadedStepInstance.formValidated.emit(this.hostFormValidated);
  }

  public goToPreviousStep(): void {
    if (!this.canGoBack) {
      return;
    }
    this.hostFormValidated = false;
    this.wizardStepIndex -= 1;
    this.loadStep(this.apiResourceSteps[this.wizardStepIndex]);
  }

  public user: CreateUserRequest = new CreateUserRequest();
  public problemDetails: ProblemDetails;

  public setSaveAndConfigure(value: boolean) {
    this._saveAndConfigure = value;
  }

  public save(): void {
    const newUser = this.form.value as CreateUserRequest;
    this._api.createUser(newUser).subscribe(
      (createdUser: SingleUserInfo) => {
        this._toast.showSuccess(
          `User '${createdUser.email}' was created successfully.`
        );
        if (this._saveAndConfigure) {
          this._router.navigateByUrl(`/app/users/${createdUser.id}/details`);
        } else {
          this._router.navigate(["../"], { relativeTo: this._route });
        }
      },
      (problemDetails: HttpValidationProblemDetails) => {
        this.problemDetails = problemDetails;
      }
    );
  }

  private validateFormFields(formGroup: UntypedFormGroup) {
    Object.keys(formGroup.controls).forEach((field: string) => {
      const control = formGroup.get(field);
      if (control instanceof UntypedFormControl) {
        control.markAsTouched({ onlySelf: true });
      } else if (control instanceof UntypedFormGroup) {
        this.validateFormFields(control);
      }
    });
  }

  private loadStep(step: WizardStepDescriptor): void {
    const componentFactory =
      this._componentFactoryResolver.resolveComponentFactory(step.component);
    const viewContainerRef = this._wizardStepHost.viewContainerRef;
    viewContainerRef.clear();
    const componentRef = viewContainerRef.createComponent(componentFactory);
    // Keep a reference of the instance of the step component.
    this._loadedStepInstance =
      componentRef.instance as StepBaseComponent<UserWizardModel>;
    // Pass data to the dynamically loaded component.
    this._loadedStepInstance.data = {
      apiResource: this.resource,
      form: this.form,
      navigationOrigin: this._navigationOrigin,
    } as UserWizardModel;
    if (this._formValidatedSubscription) {
      this._formValidatedSubscription.unsubscribe();
    }
    this._formValidatedSubscription =
      this._loadedStepInstance.formValidated.subscribe((value: boolean) => {
        this.hostFormValidated = value;
      });
  }
}
