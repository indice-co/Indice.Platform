import { Component, OnInit } from '@angular/core';

import { StepBaseComponent } from '../step-base.component';
import { IdentityResourceInfo, ApiResourceInfo } from 'src/app/core/services/identity-api.service';
import { ClientsWizardService } from '../../clients-wizard.service';
import { AbstractControl } from '@angular/forms';

@Component({
    selector: 'app-api-resources-step',
    templateUrl: './api-resources-step.component.html'
})
export class ApiResourcesStepComponent extends StepBaseComponent implements OnInit {
    private _selectedResourcesControl: AbstractControl;

    constructor(private _wizardService: ClientsWizardService) {
        super();
    }

    public availableResources: ApiResourceInfo[] = [];
    public selectedResources: ApiResourceInfo[] = [];

    public ngOnInit(): void {
        this._wizardService.getApiResources().subscribe((resources: ApiResourceInfo[]) => {
            this._selectedResourcesControl = this.data.form.controls.apiResources;
            this.availableResources = resources.filter(x => !this._selectedResourcesControl.value.includes(x.name));
            this.selectedResources = resources.filter(x => this._selectedResourcesControl.value.includes(x.name));
        });
    }

    public addResource(resource: ApiResourceInfo): void {
        const resources = this._selectedResourcesControl.value as Array<string>;
        resources.push(resource.name);
        this._selectedResourcesControl.setValue(resources);
    }

    public removeResource(resource: ApiResourceInfo): void {
        const resources = this._selectedResourcesControl.value as Array<string>;
        const index = resources.indexOf(resource.name, 0);
        if (index > -1) {
            resources.splice(index, 1);
        }
        this._selectedResourcesControl.setValue(resources);
    }

    public isValid(): boolean {
        return true;
    }
}
