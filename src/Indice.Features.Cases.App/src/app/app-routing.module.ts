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
import { CaseTypesComponent } from './features/case-types/case-types.component';
import { AdminGuardService } from './core/services/admin-guard.service';
import { CaseTypeCreateComponent } from './features/case-types/case-type-create/case-type-create.component';
import { CaseTypeEditComponent } from './features/case-types/case-type-edit/case-type-edit.component';
import { MyQueriesPageComponent } from './features/cases/my-queries-page/my-queries-page.component';

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
      {
        path: 'case-types', pathMatch: 'prefix', canActivate: [AdminGuardService],
        children: [
          { path: '', component: CaseTypesComponent },
          { path: 'create', component: CaseTypeCreateComponent, pathMatch: 'full' },
          { path: ':caseTypeId/edit', component: CaseTypeEditComponent, pathMatch: 'full' }
        ]
      }
    ]
  },
  { path: 'notifications', canActivate: [AuthGuardService], component: NotificationsComponent },

  /// PATHS FOR NEW SIDE PANE FORMS GO HERE
  { path: 'new-case', component: CaseCreatePageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { paneSize: SidePaneSize.Small25 } },
  { path: 'my-queries', component: MyQueriesPageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { paneSize: SidePaneSize.Small25 } },
  { path: 'logout', component: LogOutComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  { path: '**', component: PageNotFoundComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })], // https://stackoverflow.com/a/54098719/19162333
  exports: [RouterModule]
})
export class AppRoutingModule { }
