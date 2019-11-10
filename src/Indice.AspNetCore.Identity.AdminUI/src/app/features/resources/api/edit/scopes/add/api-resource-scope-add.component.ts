import { Component, OnInit, ViewChild, ComponentFactoryResolver, ChangeDetectorRef } from '@angular/core';
import { FormGroup, FormControl, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { WizardStepDescriptor } from 'src/app/shared/components/step-base/models/wizard-step-descriptor';
import { WizardStepDirective } from 'src/app/shared/components/step-base/wizard-step.directive';
import { StepBaseComponent } from 'src/app/shared/components/step-base/step-base.component';
import { CreateApiScopeRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ResourceWizardModel } from '../../../../add/models/resource-wizard-model';
import { ApiResourceStore } from '../../../api-resource-store.service';
import { UserClaimsStepComponent } from 'src/app/features/resources/add/wizard/steps/user-claims/user-claims-step.component';
import { BasicInfoStepComponent } from 'src/app/features/resources/add/wizard/steps/basic-info/basic-info-step.component';

@Component({
    selector: 'app-api-resource-scope-add',
    templateUrl: './api-resource-scope-add.component.html'
})
export class ApiResourceScopeAddComponent implements OnInit {
    @ViewChild(WizardStepDirective, { static: false }) private _wizardStepHost: WizardStepDirective;
    private _loadedStepInstance: StepBaseComponent<ResourceWizardModel>;
    private _formValidatedSubscription: Subscription;
    private _apiResourceId: number;

    constructor(private _componentFactoryResolver: ComponentFactoryResolver, private _formBuilder: FormBuilder, private _changeDetectionRef: ChangeDetectorRef,
                private _apiResourceStore: ApiResourceStore, private _toast: ToastService, private _router: Router, private _route: ActivatedRoute) { }

    public wizardStepIndex = 0;
    public apiResourceSteps: WizardStepDescriptor[] = [];
    public form: FormGroup;
    public hostFormValidated = false;
    public resource: CreateApiScopeRequest = new CreateApiScopeRequest();

    public get canGoFront(): boolean {
        return this.wizardStepIndex >= 0 && this.wizardStepIndex < this.apiResourceSteps.length - 1;
    }

    public get canGoBack(): boolean {
        return this.wizardStepIndex > 0 && this.wizardStepIndex <= this.apiResourceSteps.length - 1;
    }

    public get isSummaryStep(): boolean {
        return this.wizardStepIndex === this.apiResourceSteps.length - 1;
    }

    public ngOnInit(): void {
        this._apiResourceId = +this._route.parent.snapshot.params.id;
        this.form = this._formBuilder.group({
            name: ['', [Validators.required, Validators.maxLength(200)]],
            displayName: ['', [Validators.maxLength(200)]],
            description: ['', [Validators.maxLength(1000)]],
            userClaims: [[]]
        });
        this.apiResourceSteps = [
            new WizardStepDescriptor('Basic Details', BasicInfoStepComponent),
            new WizardStepDescriptor('User Claims', UserClaimsStepComponent)
        ];
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

    public saveApiResourceScope(): void {
        const resourceName = this.form.get('name').value;
        this._apiResourceStore.addApiResourceScope(this._apiResourceId, {
            name: resourceName,
            displayName: this.form.get('displayName').value,
            description: this.form.get('description').value,
            userClaims: this.form.get('userClaims').value
        } as CreateApiScopeRequest).subscribe(_ => {
            this._toast.showSuccess(`API scope '${resourceName}' was created successfully.`);
            this._router.navigate(['../'], { relativeTo: this._route });
        });
    }

    private validateFormFields(formGroup: FormGroup) {
        Object.keys(formGroup.controls).forEach((field: string) => {
            const control = formGroup.get(field);
            if (control instanceof FormControl) {
                control.markAsTouched({ onlySelf: true });
            } else if (control instanceof FormGroup) {
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
        this._loadedStepInstance = componentRef.instance as StepBaseComponent<ResourceWizardModel>;
        // Pass data to the dynamically loaded component.
        this._loadedStepInstance.data = {
            apiResource: this.resource,
            form: this.form,
            displayType: false
        } as ResourceWizardModel;
        if (this._formValidatedSubscription) {
            this._formValidatedSubscription.unsubscribe();
        }
        this._formValidatedSubscription = this._loadedStepInstance.formValidated.subscribe((value: boolean) => {
            this.hostFormValidated = value;
        });
    }
}
