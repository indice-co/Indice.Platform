import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { IdentityResourcesComponent } from './identity/identity-resources.component';
import { IdentityResourceEditComponent } from './identity/edit/identity-resource-edit.component';
import { IdentityResourceDetailsComponent } from './identity/edit/details/identity-resource-details.component';
import { IdentityResourceClaimsComponent } from './identity/edit/claims/identity-resource-claims.component';
import { IdentityResourceAddComponent } from './identity/add/identity-resource-add.component';
import { ApiResourcesComponent } from './api/api-resources.component';
import { ApiResourceEditComponent } from './api/edit/api-resource-edit.component';
import { ApiResourceDetailsComponent } from './api/edit/details/api-resource-details.component';
import { ApiResourceScopesComponent } from './api/edit/scopes/api-resource-scopes.component';
import { ApiResourceAddComponent } from './api/add/api-resource-add.component';
import { ApiResourceClaimsComponent } from './api/edit/claims/api-resource-claims.component';
import { ApiResourceScopeAddComponent } from './api/edit/scopes/add/api-resource-scope-add.component';
import { ApiResourceSecretsComponent } from './api/edit/secrets/api-resource-secrets.component';

const routes: Routes = [
    { path: '', redirectTo: 'identity', pathMatch: 'full' },
    { path: 'identity', component: IdentityResourcesComponent },
    { path: 'identity/add', component: IdentityResourceAddComponent },
    {
        path: 'identity/:id', component: IdentityResourceEditComponent, children: [
            { path: '', redirectTo: 'details', pathMatch: 'full' },
            { path: 'details', component: IdentityResourceDetailsComponent },
            { path: 'claims', component: IdentityResourceClaimsComponent }
        ]
    },
    { path: 'api', component: ApiResourcesComponent },
    { path: 'api/add', component: ApiResourceAddComponent },
    {
        path: 'api/:id', component: ApiResourceEditComponent, children: [
            { path: '', redirectTo: 'details', pathMatch: 'full' },
            { path: 'details', component: ApiResourceDetailsComponent },
            { path: 'scopes', component: ApiResourceScopesComponent },
            { path: 'scopes/add', component: ApiResourceScopeAddComponent },
            { path: 'claims', component: ApiResourceClaimsComponent },
            { path: 'secrets', component: ApiResourceSecretsComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ResourcesRoutingModule { }
