import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent, SidePaneSize } from '@indice/ng-components';
import { AuthGuardService } from '@indice/ng-auth';
import { HomeComponent } from './features/home/home.component';
import { RiskEventsComponent } from './features/risk-events/risk-events.component';
import { RiskResultsComponent } from './features/risk-results/risk-results.component';
import { RiskDetailsPageComponent } from './shared/risk-details-page/risk-details-page.component';
import { RulesListComponent } from './features/rules-list/rules-list.component';
import { RuleOptionsPageComponent } from './shared/rule-options-page/rule-options-page.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'risk-events', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, pathMatch: 'full' },
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'risk-events', pathMatch: 'full' },
      { path: 'risk-events', component: RiskEventsComponent },
      { path: 'risk-results', component: RiskResultsComponent },
      { path: 'rules', component: RulesListComponent }
    ]
  },
  { path: 'details', component: RiskDetailsPageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { paneSize: SidePaneSize.Small25 } },
  { path: 'options', component: RuleOptionsPageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { paneSize: SidePaneSize.Small25 } },
  { path: '**', component: PageNotFoundComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
