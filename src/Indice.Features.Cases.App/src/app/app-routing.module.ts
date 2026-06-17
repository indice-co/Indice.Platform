import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent, SidePaneSize, UnauthorizedComponent } from '@indice/ng-components';
import { AuthGuardService } from '@indice/ng-auth';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { HomeComponent } from './features/home/home.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { CaseDetailPageComponent } from './features/cases/case-detail-page/case-detail-page.component';
import { NotificationsComponent } from './features/notifications/notifications.component';
import { CaseCreatePageComponent } from './features/cases/case-create-page/case-create-page.component';
import { CaseTypesComponent } from './features/case-types/case-types.component';
import { adminGuard } from './core/guards/admin-guard';
import { CaseTypeCreateComponent } from './features/case-types/case-type-create/case-type-create.component';
import { CaseTypeEditComponent } from './features/case-types/case-type-edit/case-type-edit.component';
import { QueriesPageComponent } from './features/cases/queries-page/queries-page.component';
import { GeneralCasesComponent } from './features/cases/general-cases/general-cases.component';
import { CaseTypeSpecificCasesComponent } from './features/cases/case-type-specific-cases/case-type-specific-cases.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent, data: { title: 'Αυθεντικοποίηση', breadcrumb: { title: 'breadcrumb.authCallback' } } },
  { path: 'auth-renew', component: AuthRenewComponent, data: { title: 'Ανενέωση Αυθεντικοποίησης', breadcrumb: { title: 'breadcrumb.authRenew' } } },
  { path: '', redirectTo: 'home', pathMatch: 'full', data: { title: 'Αρχική', breadcrumb: { title: 'breadcrumb.home' } }  },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { title: 'Αρχική', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'breadcrumb.home' } } },
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full', data: { title: 'Dashboard', breadcrumb: { title: 'breadcrumb.dashboard' } } },
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Dashboard', breadcrumb: { title: 'breadcrumb.dashboard' } } },
      {
        path: 'cases',
        data: { title: 'Υποθέσεις', breadcrumb: { title: 'breadcrumb.cases' } },
        component: GeneralCasesComponent
      },
      {
        path: 'case/by-type/:caseTypeCode', pathMatch: 'prefix',
        data: { title: 'Υποθέσεις Συγκεκριμένου Τύπου', breadcrumb: { title: 'breadcrumb.casesByType' } },
        component: CaseTypeSpecificCasesComponent
      },
      {
        path: 'cases/:caseId', pathMatch: 'prefix', data: { title: 'Υπόθεση', breadcrumb: { title: 'breadcrumb.case' } },
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details', data: { title: 'Λεπτομέρειες Υπόθεσης', breadcrumb: { title: 'breadcrumb.caseDetails' } } },
          { path: 'details', component: CaseDetailPageComponent, data: { title: 'Λεπτομέρειες Υπόθεσης', animation: 'three', breadcrumb: { title: 'breadcrumb.caseDetails' } } }
        ]
      },
      {
        path: 'case-types', pathMatch: 'prefix', canActivate: [adminGuard], data: { title: 'Τύποι Υπόθεσης', breadcrumb: { title: 'breadcrumb.caseTypes' } },
        children: [
          { path: '', component: CaseTypesComponent, data: { title: 'Τύποι Υπόθεσης', breadcrumb: { title: 'breadcrumb.caseTypes' } } },
          { path: 'create', component: CaseTypeCreateComponent, pathMatch: 'full', data: { title: 'Δημιουργία Υπόθεσης', breadcrumb: { title: 'breadcrumb.createCaseType' } } },
          { path: ':caseTypeId/edit', component: CaseTypeEditComponent, pathMatch: 'full', data: { title: 'Επεξεργασία Τύπου Υπόθεσης', breadcrumb: { title: 'breadcrumb.editCaseType' } } }
        ]
      }
    ]
  },
  { path: 'notifications', canActivate: [AuthGuardService], component: NotificationsComponent, data: { title: 'Notifications', breadcrumb: { title: 'breadcrumb.notifications' } } },

  /// PATHS FOR NEW SIDE PANE FORMS GO HERE
  { path: 'new-case', component: CaseCreatePageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { title: 'New Case', paneSize: SidePaneSize.Small25, breadcrumb: { title: 'breadcrumb.newCase' } } },
  { path: 'queries', component: QueriesPageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { title: 'Queries', paneSize: SidePaneSize.Small25, breadcrumb: { title: 'breadcrumb.queries' } } },
  { path: 'logout', component: LogOutComponent, data: { title: 'Logout', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'breadcrumb.logout' } } },
  { path: 'forbidden', component: UnauthorizedComponent, data: { title: 'Forbidden', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'breadcrumb.forbidden' } } },
  { path: '**', component: PageNotFoundComponent, data: { title: 'Page Not Found', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'breadcrumb.pageNotFound' } } },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })], // https://stackoverflow.com/a/54098719/19162333
  exports: [RouterModule]
})
export class AppRoutingModule { }
