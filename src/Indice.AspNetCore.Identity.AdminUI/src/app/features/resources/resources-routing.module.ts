import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { IdentityResourcesComponent } from './identity/identity-resources.component';
import { IdentityResourceEditComponent } from './identity/edit/identity-resource-edit.component';
import { IdentityResourceDetailsComponent } from './identity/edit/details/identity-resource-details.component';
import { IdentityResourceClaimsComponent } from './identity/edit/claims/identity-resource-claims.component';
import { IdentityResourceAddComponent } from './identity/add/identity-resource-add.component';
import { ApiResourcesComponent } from './api/api-resources.component';

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
    { path: 'api', component: ApiResourcesComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ResourcesRoutingModule { }
