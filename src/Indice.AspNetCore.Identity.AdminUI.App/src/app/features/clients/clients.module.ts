import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { NgbDropdownModule, NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { ApiResourcesStepComponent } from './add/wizard/steps/api-resources/api-resources-step.component';
import { BasicInfoStepComponent } from './add/wizard/steps/basic-info/basic-info-step.component';
import { Bootstrap4FrameworkModule } from '@ajsf/bootstrap4';
import { ClientAddComponent } from './add/client-add.component';
import { ClientAdvancedComponent } from './edit/advanced/client-advanced.component';
import { ClientApiResourcesComponent } from './edit/resources/api/client-api-resources.component';
import { ClientClaimsComponent } from './edit/advanced/claims/client-claims.component';
import { ClientDetailsComponent } from './edit/details/client-details.component';
import { ClientEditComponent } from './edit/client-edit.component';
import { ClientGrantTypesComponent } from './edit/advanced/grant-types/client-grant-types.component';
import { ClientIdentityResourcesComponent } from './edit/resources/identity/client-identity-resources.component';
import { ClientResourcesComponent } from './edit/resources/client-resources.component';
import { ClientsComponent } from './clients.component';
import { ClientSecretsComponent } from './edit/secrets/client-secrets.component';
import { ClientsRoutingModule } from './clients-routing.module';
import { ClientTokensComponent } from './edit/advanced/tokens/client-tokens.component';
import { ClientUiConfigComponent } from './edit/ui-config/ui-config.component';
import { ClientUrlsComponent } from './edit/urls/client-urls.component';
import { ExtendedInfoStepComponent } from './add/wizard/steps/extended-info/extended-info-step.component';
import { IdentityResourcesStepComponent } from './add/wizard/steps/identity-resources/identity-resources-step.component';
import { SecretsStepComponent } from './add/wizard/steps/secrets/secrets-step.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { SummaryStepComponent } from './add/wizard/steps/summary/summary-step.component';
import { UrlsStepComponent } from './add/wizard/steps/urls/urls-step.component';

@NgModule({
    declarations: [
        ApiResourcesStepComponent,
        BasicInfoStepComponent,
        ClientAddComponent,
        ClientAdvancedComponent,
        ClientApiResourcesComponent,
        ClientClaimsComponent,
        ClientDetailsComponent,
        ClientEditComponent,
        ClientGrantTypesComponent,
        ClientIdentityResourcesComponent,
        ClientResourcesComponent,
        ClientsComponent,
        ClientSecretsComponent,
        ClientTokensComponent,
        ClientUiConfigComponent,
        ClientUrlsComponent,
        ExtendedInfoStepComponent,
        IdentityResourcesStepComponent,
        SecretsStepComponent,
        SummaryStepComponent,
        UrlsStepComponent
    ],
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        ClientsRoutingModule,
        NgbDropdownModule,
        SharedModule,
        SweetAlert2Module,
        NgbDatepickerModule,
        Bootstrap4FrameworkModule
    ]
})
export class ClientsModule { }
