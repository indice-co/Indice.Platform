import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { ResourcesRoutingModule } from './resources-routing.module';
import { IdentityResourcesComponent } from './identity/identity-resources.component';
import { IdentityResourceEditComponent } from './identity/edit/identity-resource-edit.component';
import { IdentityResourceDetailsComponent } from './identity/edit/details/identity-resource-details.component';
import { IdentityResourceClaimsComponent } from './identity/edit/claims/identity-resource-claims.component';
import { IdentityResourceAddComponent } from './identity/add/identity-resource-add.component';
import { ApiResourcesComponent } from './api/api-resources.component';
import { ApiResourceEditComponent } from './api/edit/api-resource-edit.component';
import { ApiResourceDetailsComponent } from './api/edit/details/api-resource-details.component';
import { ApiResourceScopesComponent } from './api/edit/scopes/api-resource-scopes.component';
import { ApiResourceScopeDetailsComponent } from './api/edit/scopes/details/api-resource-scope-details.component';
import { ApiResourceScopeClaimsComponent } from './api/edit/scopes/claims/api-resource-scope-claims.component';
import { ApiResourceAddComponent } from './api/add/api-resource-add.component';
import { ApiResourceClaimsComponent } from './api/edit/claims/api-resource-claims.component';
import { ApiResourceScopeAddComponent } from './api/edit/scopes/add/api-resource-scope-add.component';
import { UserClaimsStepComponent } from './api/wizard/steps/user-claims/user-claims-step.component';
import { BasicInfoStepComponent } from './api/wizard/steps/basic-info/basic-info-step.component';
import { ApiResourceSecretsComponent } from './api/edit/secrets/api-resource-secrets.component';
import { NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';

@NgModule({
    declarations: [
        IdentityResourcesComponent,
        IdentityResourceEditComponent,
        IdentityResourceDetailsComponent,
        IdentityResourceClaimsComponent,
        IdentityResourceAddComponent,
        ApiResourcesComponent,
        ApiResourceEditComponent,
        ApiResourceDetailsComponent,
        ApiResourceScopesComponent,
        ApiResourceScopeDetailsComponent,
        ApiResourceScopeClaimsComponent,
        ApiResourceAddComponent,
        BasicInfoStepComponent,
        UserClaimsStepComponent,
        ApiResourceClaimsComponent,
        ApiResourceScopeAddComponent,
        ApiResourceSecretsComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ResourcesRoutingModule,
        SharedModule,
        SweetAlert2Module,
        NgbDatepickerModule
    ],
    entryComponents: [
        BasicInfoStepComponent,
        UserClaimsStepComponent
    ]
})
export class ResourcesModule { }
