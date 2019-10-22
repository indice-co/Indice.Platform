import { Component, OnInit } from '@angular/core';

import { StepBaseComponent } from '../../../../../../shared/components/step-base/step-base.component';
import { CreateClientRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ClientWizardModel } from '../../models/client-wizard-model';

@Component({
    selector: 'app-summary-step',
    templateUrl: './summary-step.component.html'
})
export class SummaryStepComponent extends StepBaseComponent<ClientWizardModel> implements OnInit {
    constructor(public _toast: ToastService) {
        super();
    }

    public summary: CreateClientRequest = new CreateClientRequest();

    public ngOnInit(): void {
        const form = this.data.form;
        this.summary = {
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

    public isValid(): boolean {
        return true;
    }
}
