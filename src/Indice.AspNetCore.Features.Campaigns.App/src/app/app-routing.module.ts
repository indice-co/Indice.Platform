import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuardService } from '@indice/ng-auth';
import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent } from '@indice/ng-components';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CampaignUpsertComponent } from './features/campaigns/create/campaign-create.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'app', pathMatch: 'full' },
  {
    path: 'app', canActivate: [AuthGuardService], children: [
      // { path: '' },
      { path: 'campaigns', component: CampaignsComponent },
      { path: 'campaigns/create', component: CampaignUpsertComponent }
    ]
  },
  {
    path: '**',
    component: PageNotFoundComponent,
    data: { shell: { fluid: true, showHeader: false, showFooter: false } }
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
