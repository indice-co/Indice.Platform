import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent, SidePaneSize } from '@indice/ng-components';
import { AuthGuardService } from '@indice/ng-auth';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { HomeComponent } from './features/home/home.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { CasesComponent } from './features/cases/cases.component';
import { CaseDetailPageComponent } from './features/cases/case-detail-page/case-detail-page.component';
import { NotificationsComponent } from './features/notifications/notifications.component';
import { CaseCreatePageComponent } from './features/cases/case-create-page/case-create-page.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'cases', component: CasesComponent },
      {
        path: 'cases/:caseId', pathMatch: 'prefix',
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          { path: 'details', component: CaseDetailPageComponent, data: { animation: 'three' } }
        ]
      },
      
    ]
  },
  { path: 'notifications', canActivate: [AuthGuardService], component: NotificationsComponent }, 

  /// PATHS FOR NEW SIDE PANE FORMS GO HERE
  { path: 'new-case', component: CaseCreatePageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { paneSize: SidePaneSize.Small25 } },
  { path: 'logout', component: LogOutComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  { path: '**', component: PageNotFoundComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
