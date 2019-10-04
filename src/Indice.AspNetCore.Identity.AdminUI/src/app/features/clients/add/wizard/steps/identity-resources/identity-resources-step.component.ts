import { Component, OnInit } from '@angular/core';

import { StepBaseComponent } from '../step-base.component';
import { IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { ClientsWizardService } from '../../clients-wizard.service';
import { AbstractControl } from '@angular/forms';

@Component({
    selector: 'app-identity-resources-step',
    templateUrl: './identity-resources-step.component.html'
})
export class IdentityResourcesStepComponent extends StepBaseComponent implements OnInit {
    private _selectedResourcesControl: AbstractControl;

    constructor(private _wizardService: ClientsWizardService) {
        super();
    }

    public availableResources: IdentityResourceInfo[] = [];
    public selectedResources: IdentityResourceInfo[] = [];

    public ngOnInit(): void {
        this._wizardService.getIdentityResources().subscribe((resources: IdentityResourceInfo[]) => {
            this._selectedResourcesControl = this.data.form.controls.identityResources;
            this.availableResources = resources.filter(x => !this._selectedResourcesControl.value.includes(x.name));
            this.selectedResources = resources.filter(x => this._selectedResourcesControl.value.includes(x.name));
        });
    }

    public addResource(resource: IdentityResourceInfo): void {
        const resources = this._selectedResourcesControl.value as Array<string>;
        resources.push(resource.name);
        this._selectedResourcesControl.setValue(resources);
    }

    public removeResource(resource: IdentityResourceInfo): void {
        const resources = this._selectedResourcesControl.value as Array<string>;
        const index = resources.indexOf(resource.name, 0);
        if (index > -1) {
            resources.splice(index, 1);
        }
        this._selectedResourcesControl.setValue(resources);
    }

    public isValid(): boolean {
        const resources = this.data.form.controls.identityResources.value as Array<string>;
        return resources.length > 0;
    }
}
