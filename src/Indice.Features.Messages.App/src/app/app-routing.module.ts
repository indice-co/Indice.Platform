import { CampaignsRemoveComponent } from './features/campaigns/manage/remove/campaigns-remove.component';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent } from '@indice/ng-components';
import { AuthGuardService } from '@indice/ng-auth';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { HomeComponent } from './features/home/home.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { CampaignsManageComponent } from './features/campaigns/manage/campaigns-manage.component';
import { CampaignsDetailsComponent } from './features/campaigns/manage/details/campaigns-details.component';
import { MessageTypesComponent } from './features/message-types/message-types.component';
import { MessageTypeCreateComponent } from './features/message-types/create/message-type-create.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'campaigns', component: CampaignsComponent },
      {
        path: 'campaigns/:campaignId', component: CampaignsManageComponent, pathMatch: 'prefix', children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          { path: 'details', component: CampaignsDetailsComponent, data: { animation: 'three' } },
          { path: 'manage', component: CampaignsRemoveComponent, data: { animation: 'three' } }
        ]
      },
      { path: 'message-types', component: MessageTypesComponent }
    ]
  },
  { path: 'create-campaign', canActivate: [AuthGuardService], component: CampaignCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'create-message-type', canActivate: [AuthGuardService], component: MessageTypeCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'logout', component: LogOutComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  { path: '**', component: PageNotFoundComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
