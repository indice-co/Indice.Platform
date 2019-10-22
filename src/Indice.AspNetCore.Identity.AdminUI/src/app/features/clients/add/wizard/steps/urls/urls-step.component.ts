import { Component, OnInit } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { StepBaseComponent } from 'src/app/shared/components/step-base/step-base.component';
import { ClientWizardModel } from '../../models/client-wizard-model';

@Component({
    selector: 'app-urls-step',
    templateUrl: './urls-step.component.html'
})
export class UrlsStepComponent extends StepBaseComponent<ClientWizardModel> implements OnInit {
    constructor() {
        super();
    }

    public get callbackUrl(): AbstractControl {
        return this.data.form.get('callbackUrl');
    }

    public get postLogoutUrl(): AbstractControl {
        return this.data.form.get('postLogoutUrl');
    }

    public ngOnInit(): void { }

    public isValid(): boolean {
        return this.callbackUrl.valid && this.postLogoutUrl.valid;
    }
}
