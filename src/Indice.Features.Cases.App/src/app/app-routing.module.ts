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
import { adminGuard } from './core/guards/admin-guard';
import { CaseTypeCreateComponent } from './features/case-types/case-type-create/case-type-create.component';
import { CaseTypeEditComponent } from './features/case-types/case-type-edit/case-type-edit.component';
import { QueriesPageComponent } from './features/cases/queries-page/queries-page.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent, data: { title: 'Auth Callback' } },
  { path: 'auth-renew', component: AuthRenewComponent, data: { title: 'Auth Renew' } },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { title: 'Home', shell: { fluid: true, showHeader: false, showFooter: false } } },
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Dashboard' } },
      { path: 'cases', component: CasesComponent, data: { title: 'Cases' } },
      {
        path: 'cases/:caseId', pathMatch: 'prefix', data: { title: 'Case Details' },
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          { path: 'details', component: CaseDetailPageComponent, data: { title: 'Case Details', animation: 'three' } }
        ]
      },
      {
        path: 'case-types', pathMatch: 'prefix', canActivate: [adminGuard], data: { title: 'Case Types' },
        children: [
          { path: '', component: CaseTypesComponent, data: { title: 'Case Types' } },
          { path: 'create', component: CaseTypeCreateComponent, pathMatch: 'full', data: { title: 'Create Case Type' } },
          { path: ':caseTypeId/edit', component: CaseTypeEditComponent, pathMatch: 'full', data: { title: 'Edit Case Type' } }
        ]
      }
    ]
  },
  { path: 'notifications', canActivate: [AuthGuardService], component: NotificationsComponent, data: { title: 'Notifications' } },

  /// PATHS FOR NEW SIDE PANE FORMS GO HERE
  { path: 'new-case', component: CaseCreatePageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { title: 'New Case', paneSize: SidePaneSize.Small25 } },
  { path: 'queries', component: QueriesPageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { title: 'Queries', paneSize: SidePaneSize.Small25 } },
  { path: 'logout', component: LogOutComponent, data: { title: 'Logout', shell: { fluid: true, showHeader: false, showFooter: false } } },
  { path: '**', component: PageNotFoundComponent, data: { title: 'Page Not Found', shell: { fluid: true, showHeader: false, showFooter: false } } },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })], // https://stackoverflow.com/a/54098719/19162333
  exports: [RouterModule]
})
export class AppRoutingModule { }
