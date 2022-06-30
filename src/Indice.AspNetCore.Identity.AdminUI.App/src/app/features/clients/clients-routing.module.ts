import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

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
import { ClientTokensComponent } from './edit/advanced/tokens/client-tokens.component';
import { ClientUiConfigComponent } from './edit/ui-config/ui-config.component';
import { ClientUrlsComponent } from './edit/urls/client-urls.component';

const routes: Routes = [
  { path: '', component: ClientsComponent },
  { path: 'add', component: ClientAddComponent },
  {
    path: ':id', component: ClientEditComponent, children: [
      { path: '', redirectTo: 'details', pathMatch: 'full' },
      { path: 'details', component: ClientDetailsComponent },
      { path: 'urls', component: ClientUrlsComponent },
      { path: 'secrets', component: ClientSecretsComponent },
      {
        path: 'resources', component: ClientResourcesComponent, children: [
          { path: '', redirectTo: 'api', pathMatch: 'full' },
          { path: 'api', component: ClientApiResourcesComponent },
          { path: 'identity', component: ClientIdentityResourcesComponent }
        ]
      },
      {
        path: 'advanced', component: ClientAdvancedComponent, children: [
          { path: '', redirectTo: 'tokens', pathMatch: 'full' },
          { path: 'tokens', component: ClientTokensComponent },
          { path: 'claims', component: ClientClaimsComponent },
          { path: 'grant-types', component: ClientGrantTypesComponent }
        ]
      },
      { path: 'ui-config', component: ClientUiConfigComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ClientsRoutingModule { }
