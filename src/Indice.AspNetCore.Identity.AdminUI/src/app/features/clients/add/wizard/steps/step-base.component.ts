
import { Output, EventEmitter, Input } from '@angular/core';

import { WizardFormModel } from '../models/wizard-form-model';
import { CreateClientRequest } from 'src/app/core/services/identity-api.service';

export abstract class StepBaseComponent {
    constructor() {
        this.formValidated.subscribe((value: boolean) => {
            this.hostFormValidated = value;
        });
    }

    @Output() public formValidated = new EventEmitter<boolean>();
    @Input() public data: WizardFormModel;
    public hostFormValidated: boolean;
    public abstract isValid(): boolean;

    public getSummary(): CreateClientRequest {
        const form = this.data.form;
        return {
            clientType: form.get('clientType').value,
            clientId: form.get('clientId').value,
            clientName: form.get('clientName').value,
            requireConsent: form.get('requireConsent').value,
            clientUri: form.get('clientUrl').value,
            logoUri: form.get('logoUrl').value,
            description: form.get('description').value,
            redirectUri: form.get('callbackUrl').value,
            postLogoutRedirectUri: form.get('postLogoutUrl').value,
            identityResources: form.get('identityResources').value,
            apiResources: form.get('apiResources').value,
            secrets: form.get('secrets').value
        } as CreateClientRequest;
    }
}
