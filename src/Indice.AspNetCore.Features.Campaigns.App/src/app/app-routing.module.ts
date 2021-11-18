import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuardService } from '@indice/ng-auth';
import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent, SidePaneSize } from '@indice/ng-components';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'app/dashboard', pathMatch: 'full' },
  {
    path: 'app', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'campaigns', component: CampaignsComponent }
    ]
  },
  { path: 'create', canActivate: [AuthGuardService], component: CampaignCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
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
