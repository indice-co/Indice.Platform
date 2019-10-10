import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { NgbDropdownModule, NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { ClientsComponent } from './clients.component';
import { ClientsRoutingModule } from './clients-routing.module';
import { ClientAddComponent } from './add/client-add.component';
import { ExtendedInfoStepComponent } from './add/wizard/steps/extended-info/extended-info-step.component';
import { WizardStepDirective } from './add/wizard/wizard-step.directive';
import { BasicInfoStepComponent } from './add/wizard/steps/basic-info/basic-info-step.component';
import { UrlsStepComponent } from './add/wizard/steps/urls/urls-step.component';
import { IdentityResourcesStepComponent } from './add/wizard/steps/identity-resources/identity-resources-step.component';
import { ApiResourcesStepComponent } from './add/wizard/steps/api-resources/api-resources-step.component';
import { SummaryStepComponent } from './add/wizard/steps/summary/summary-step.component';
import { SecretsStepComponent } from './add/wizard/steps/secrets/secrets-step.component';
import { ClientEditComponent } from './edit/client-edit.component';
import { ClientDetailsComponent } from './edit/details/client-details.component';
import { ClientUrlsComponent } from './edit/urls/client-urls.component';
import { ClientResourcesComponent } from './edit/resources/client-resources.component';
import { ClientApiResourcesComponent } from './edit/resources/api/client-api-resources.component';
import { ClientIdentityResourcesComponent } from './edit/resources/identity/client-identity-resources.component';

@NgModule({
    declarations: [
        ClientsComponent,
        ClientAddComponent,
        ExtendedInfoStepComponent,
        BasicInfoStepComponent,
        UrlsStepComponent,
        IdentityResourcesStepComponent,
        ApiResourcesStepComponent,
        SecretsStepComponent,
        SummaryStepComponent,
        ClientEditComponent,
        ClientDetailsComponent,
        ClientUrlsComponent,
        ClientResourcesComponent,
        ClientApiResourcesComponent,
        ClientIdentityResourcesComponent,
        WizardStepDirective
    ],
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        ClientsRoutingModule,
        NgbDropdownModule,
        SharedModule,
        SweetAlert2Module,
        NgbDatepickerModule
    ],
    // Add here any components that are dynamically loaded.
    entryComponents: [
        ExtendedInfoStepComponent,
        BasicInfoStepComponent,
        UrlsStepComponent,
        IdentityResourcesStepComponent,
        ApiResourcesStepComponent,
        SecretsStepComponent,
        SummaryStepComponent
    ]
})
export class ClientsModule { }
