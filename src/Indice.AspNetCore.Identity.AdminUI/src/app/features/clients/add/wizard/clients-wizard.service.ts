import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { ClientType } from './models/client-type';
import { WizardStepDescriptor } from '../../../../shared/components/step-base/models/wizard-step-descriptor';
import { ExtendedInfoStepComponent } from './steps/extended-info/extended-info-step.component';
import { BasicInfoStepComponent } from './steps/basic-info/basic-info-step.component';
import { UrlsStepComponent } from './steps/urls/urls-step.component';
import { IdentityResourcesStepComponent } from './steps/identity-resources/identity-resources-step.component';
import { ApiResourcesStepComponent } from './steps/api-resources/api-resources-step.component';
import { SummaryStepComponent } from './steps/summary/summary-step.component';
import { SecretsStepComponent } from './steps/secrets/secrets-step.component';
import { IdentityResourceInfo, ApiResourceInfo, IdentityResourceInfoResultSet, IdentityApiService, ApiResourceInfoResultSet, ClientType as Type } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ClientsWizardService {
    private _clientTypes: ClientType[] = [];
    private _clientTypeSteps: { key: string, steps: WizardStepDescriptor[] }[] = [];
    private _identityResources: AsyncSubject<IdentityResourceInfo[]>;
    private _apiResources: AsyncSubject<ApiResourceInfo[]>;

    constructor(private _api: IdentityApiService) {
        this._clientTypes.push(...[
            new ClientType(Type.SPA, 'Single Page App', 'Authorization code flow + PKCE', `A client-side application running in a browser.<br />These applications cannot be trusted to keep secrets
                or use refresh tokens.<br />Tokens will be sent through via the browser and will be visible to the user.`, 'dripicons-web'),
            new ClientType(Type.WebApp, 'Web App', 'Hybrid flow with client authentication', `A server-side application running on your infrastructure.<br />These applications can be trusted to
                keep a secret and use refresh tokens.<br />Tokens will be sent to a back-end server and will not be visible to the user.`, 'dripicons-monitor'),
            new ClientType(Type.Native, 'Native', 'Authorization code flow + PKCE', `A desktop or mobile application running on a user's device.<br />These applications cannot be trusted to keep
                a secret but can use refresh tokens.<br />Token visibility dependent on browser and redirect URI choice.`, 'dripicons-device-mobile'),
            new ClientType(Type.Machine, 'Machine', 'Client credentials', `A machine-to-machine method of communication.<br />No users are involved in the process.<br />Tokens will be sent
                via back-channel communication.`, 'dripicons-stack'),
            new ClientType(Type.Device, 'Device', 'Device flow using external browser', `An IoT application or otherwise browserless or input constrained device.<br />These applications
                typically cannot be trusted to keep a secret but can use refresh tokens.`, 'dripicons-wifi'),
            new ClientType(Type.SPALegacy, 'Single Page App (legacy)', 'Implicit flow', `A client-side application running in a browser using previous OAuth Working Group recommendations.
                <br />These applications cannot be trusted to keep secrets or use refresh tokens.<br />Tokens will be sent through via the browser and will be visible to the user.`,
                'dripicons-web')
        ]);
        this._clientTypeSteps.push(...[{
            key: Type.SPA, steps: [...[
                new WizardStepDescriptor('Basic details', ExtendedInfoStepComponent),
                new WizardStepDescriptor('Application URLs', UrlsStepComponent),
                new WizardStepDescriptor('Access to Identity resources', IdentityResourcesStepComponent),
                new WizardStepDescriptor('Access to API resources', ApiResourcesStepComponent),
                new WizardStepDescriptor('Summary', SummaryStepComponent)
            ]]
        }, {
            key: Type.WebApp, steps: [...[
                new WizardStepDescriptor('Basic details', ExtendedInfoStepComponent),
                new WizardStepDescriptor('Application URLs', UrlsStepComponent),
                new WizardStepDescriptor('Secrets', SecretsStepComponent),
                new WizardStepDescriptor('Access to Identity resources', IdentityResourcesStepComponent),
                new WizardStepDescriptor('Access to API resources', ApiResourcesStepComponent),
                new WizardStepDescriptor('Summary', SummaryStepComponent)
            ]]
        }, {
            key: Type.Native, steps: [...[
                new WizardStepDescriptor('Basic details', ExtendedInfoStepComponent)
            ]]
        }, {
            key: Type.Machine, steps: [...[
                new WizardStepDescriptor('Basic details', BasicInfoStepComponent),
                new WizardStepDescriptor('Access to API resources', ApiResourcesStepComponent),
                new WizardStepDescriptor('Secrets', SecretsStepComponent),
                new WizardStepDescriptor('Summary', SummaryStepComponent)
            ]]
        }, {
            key: Type.Device, steps: [...[
                new WizardStepDescriptor('Basic details', BasicInfoStepComponent)
            ]]
        }, {
            key: Type.SPALegacy, steps: [...[
                new WizardStepDescriptor('Basic details', ExtendedInfoStepComponent)
            ]]
        }]);
    }

    public getClientTypes(): ClientType[] {
        return this._clientTypes;
    }

    public getClientTypeSteps(key: string): WizardStepDescriptor[] {
        return this._clientTypeSteps.find(x => x.key === key).steps;
    }

    public getIdentityResources(): Observable<IdentityResourceInfo[]> {
        if (!this._identityResources) {
            this._identityResources = new AsyncSubject<IdentityResourceInfo[]>();
            this._api.getIdentityResources(1, 2147483647, 'name+', undefined).subscribe((response: IdentityResourceInfoResultSet) => {
                this._identityResources.next(response.items);
                this._identityResources.complete();
            });
        }
        return this._identityResources;
    }

    public getApiResources(): Observable<ApiResourceInfo[]> {
        if (!this._apiResources) {
            this._apiResources = new AsyncSubject<ApiResourceInfo[]>();
            this._api.getProtectedResources(1, 2147483647, 'name+', undefined).subscribe((response: ApiResourceInfoResultSet) => {
                this._apiResources.next(response.items);
                this._apiResources.complete();
            });
        }
        return this._apiResources;
    }
}
