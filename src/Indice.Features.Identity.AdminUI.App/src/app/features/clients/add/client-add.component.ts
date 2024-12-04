import { Component, OnInit, ChangeDetectorRef, ViewChild, ComponentFactoryResolver } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators, UntypedFormControl } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';

import { forkJoin, of, Subscription } from 'rxjs';
import { concatMap } from 'rxjs/operators';
import { ClientsWizardService } from './wizard/clients-wizard.service';
import { ClientType } from './wizard/models/client-type';
import { WizardStepDescriptor } from 'src/app/shared/components/step-base/models/wizard-step-descriptor';
import { WizardStepDirective } from '../../../shared/components/step-base/wizard-step.directive';
import { CreateClientRequest, IdentityApiService, ClientInfo, ClientSecretInfo, SecretInfo, FileParameter, CreateSecretRequest, HttpValidationProblemDetails } from 'src/app/core/services/identity-api.service';
import { ClientWizardModel } from './wizard/models/client-wizard-model';
import { StepBaseComponent } from 'src/app/shared/components/step-base/step-base.component';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ValidationSummaryComponent } from 'src/app/shared/components/validation-summary/validation-summary.component';

@Component({
  selector: 'app-client-add',
  templateUrl: './client-add.component.html',
  styleUrls: ['./client-add.component.scss'],
  providers: [ClientsWizardService]
})
export class ClientAddComponent implements OnInit {
  @ViewChild(WizardStepDirective, { static: false }) private _wizardStepHost: WizardStepDirective;
  private _loadedStepInstance: StepBaseComponent<ClientWizardModel>;
  private _formValidatedSubscription: Subscription;
  @ViewChild('validationSummary', { static: false }) private _validationSummary: ValidationSummaryComponent;

  constructor(
    private _wizardService: ClientsWizardService,
    private _changeDetectionRef: ChangeDetectorRef,
    private _componentFactoryResolver: ComponentFactoryResolver,
    private _formBuilder: UntypedFormBuilder,
    private _api: IdentityApiService,
    private _router: Router,
    private _route: ActivatedRoute,
    private _toast: ToastService
  ) { }

  public clientTypes: ClientType[] = [];
  public selectedClientType: ClientType;
  public clientConfigurationStarted = false;
  public clientTypeSteps: WizardStepDescriptor[] = [];
  public wizardStepIndex = 0;
  public client: CreateClientRequest = new CreateClientRequest();
  public form: UntypedFormGroup;
  public hostFormValidated = false;
  public problemDetails: HttpValidationProblemDetails;

  public get canGoFront(): boolean {
    return this.wizardStepIndex >= 0 && this.wizardStepIndex < this.clientTypeSteps.length - 1;
  }

  public get canGoBack(): boolean {
    return this.wizardStepIndex > 0 && this.wizardStepIndex <= this.clientTypeSteps.length - 1;
  }

  public get isFinishingStep(): boolean {
    return this.wizardStepIndex === this.clientTypeSteps.length - 2;
  }

  public get isSummaryStep(): boolean {
    return this.wizardStepIndex === this.clientTypeSteps.length - 1;
  }

  public ngOnInit(): void {
    this.clientTypes = this._wizardService.getClientTypes();
    this.form = this._formBuilder.group({
      clientType: [''],
      clientId: ['', [Validators.required, Validators.maxLength(200)]],
      clientName: ['', [Validators.required, Validators.maxLength(200)]],
      clientUrl: ['', Validators.maxLength(2000)],
      logoUrl: ['', Validators.maxLength(2000)],
      description: ['', Validators.maxLength(1000)],
      requireConsent: [false],
      callbackUrl: ['', [Validators.required, Validators.maxLength(2000)]],
      postLogoutUrl: ['', Validators.maxLength(2000)],
      identityResources: [[]],
      apiResources: [[]],
      secrets: [[]],
      certificates: [[]]
    });
  }

  public selectClientType(clientType: ClientType): void {
    this.selectedClientType = clientType;
  }

  public startClientConfiguration(): void {
    this.clientConfigurationStarted = true;
    this.clientTypeSteps = this._wizardService.getClientTypeSteps(this.selectedClientType.key);
    this._changeDetectionRef.detectChanges();
    this.form.get('clientType').setValue(this.selectedClientType.key);
    this.loadStep(this.clientTypeSteps[this.wizardStepIndex]);
  }

  public goToNextStep(): void {
    if (!this.canGoFront) {
      return;
    }
    if (this._loadedStepInstance.isValid()) {
      this.hostFormValidated = false;
      this.wizardStepIndex += 1;
      this.loadStep(this.clientTypeSteps[this.wizardStepIndex]);
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
    this.loadStep(this.clientTypeSteps[this.wizardStepIndex]);
  }

  public saveClient(): void {
    this._validationSummary.clear();
    const createClientRequest = {
      clientType: this.form.get('clientType').value,
      clientId: this.form.get('clientId').value,
      clientName: this.form.get('clientName').value,
      requireConsent: this.form.get('requireConsent').value,
      clientUri: this.form.get('clientUrl').value,
      logoUri: this.form.get('logoUrl').value,
      description: this.form.get('description').value,
      redirectUri: this.form.get('callbackUrl').value,
      postLogoutRedirectUri: this.form.get('postLogoutUrl').value,
      identityResources: this.form.get('identityResources').value,
      apiResources: this.form.get('apiResources').value,
      secrets: (this.form.get('secrets').value as CreateSecretRequest[]).filter(x => (x as any).type === 'SharedSecret')
    } as CreateClientRequest;
    this._api
      .createClient(createClientRequest)
      .pipe(concatMap((createdClient: ClientInfo) => {
        const filesToUpload = this.form.get('certificates').value as File[];
        if (filesToUpload.length === 0) {
          return of(new Array<SecretInfo>());
        }
        const uploads = filesToUpload.map((file: File) => {
          const fileParameter: FileParameter = { data: file, fileName: file.name };
          return this._api.uploadCertificate(createClientRequest.clientId, fileParameter);
        });
        return forkJoin(uploads);
      }))
      .subscribe(_ => {
        this._toast.showSuccess(`Client '${createClientRequest.clientId}' was created successfully.`);
        this._router.navigate(['../'], { relativeTo: this._route });
      }, (problemDetails: HttpValidationProblemDetails) => {
        this.problemDetails = problemDetails;
      });
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
    const componentFactory = this._componentFactoryResolver.resolveComponentFactory(step.component);
    const viewContainerRef = this._wizardStepHost.viewContainerRef;
    viewContainerRef.clear();
    const componentRef = viewContainerRef.createComponent(componentFactory);
    // Keep a reference of the instance of the step component.
    this._loadedStepInstance = componentRef.instance as StepBaseComponent<ClientWizardModel>;
    // Pass data to the dynamically loaded component.
    this._loadedStepInstance.data = {
      client: this.client,
      form: this.form
    } as ClientWizardModel;
    if (this._formValidatedSubscription) {
      this._formValidatedSubscription.unsubscribe();
    }
    this._formValidatedSubscription = this._loadedStepInstance.formValidated.subscribe((value: boolean) => {
      this.hostFormValidated = value;
    });
  }
}
