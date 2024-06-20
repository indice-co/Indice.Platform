import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent, SidePaneSize } from '@indice/ng-components';
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
  { path: 'auth-callback', component: AuthCallbackComponent, data: { title: 'Αυθεντικοποίηση', breadcrumb: { title: 'Αυθεντικοποίηση' } } },
  { path: 'auth-renew', component: AuthRenewComponent, data: { title: 'Ανενέωση Αυθεντικοποίησης', breadcrumb: { title: 'Ανενέωση Αυθεντικοποίησης' } } },
  { path: '', redirectTo: 'home', pathMatch: 'full', data: { title: 'Αρχική', breadcrumb: { title: 'Αρχική' } }  },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { title: 'Αρχική', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'Αρχική' } } },
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full', data: { title: 'Dashboard', breadcrumb: { title: 'Dashboard' } } },
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Dashboard', breadcrumb: { title: 'Dashboard' } } },
      {
        path: 'cases',
        data: { title: 'Υποθέσεις', breadcrumb: { title: 'Υποθέσεις' } },
        component: GeneralCasesComponent
      },
      {
        path: 'cases/type/:caseTypeCode', pathMatch: 'prefix',
        data: { title: 'Υποθέσεις Συγκεκριμένου Τύπου', breadcrumb: { title: 'Υποθέσεις Συγκεκριμένου Τύπου' } },
        component: CaseTypeSpecificCasesComponent
      },
      {
        path: 'cases/:caseId', pathMatch: 'prefix', data: { title: 'Υπόθεση', breadcrumb: { title: 'Υπόθεση' } },
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details', data: { title: 'Λεπτομέριες Υπόθεσης', breadcrumb: { title: 'Λεπτομέριες Υπόθεσης' } } },
          { path: 'details', component: CaseDetailPageComponent, data: { title: 'Λεπτομέριες Υπόθεσης', animation: 'three', breadcrumb: { title: 'Λεπτομέριες Υπόθεσης' } } }
        ]
      },
      {
        path: 'case-types', pathMatch: 'prefix', canActivate: [adminGuard], data: { title: 'Τύποι Υπόθεσης', breadcrumb: { title: 'Τύποι Υπόθεσης' } },
        children: [
          { path: '', component: CaseTypesComponent, data: { title: 'Τύποι Υπόθεσης', breadcrumb: { title: 'Τύποι Υπόθεσης' } } },
          { path: 'create', component: CaseTypeCreateComponent, pathMatch: 'full', data: { title: 'Δημιουργία Υπόθεσης', breadcrumb: { title: 'Δημιουργία Υπόθεσης' } } },
          { path: ':caseTypeId/edit', component: CaseTypeEditComponent, pathMatch: 'full', data: { title: 'Επεξεργασία Τύπου Υπόθεσης', breadcrumb: { title: 'Επεξεργασία Τύπου Υπόθεσης' } } }
        ]
      }
    ]
  },
  { path: 'notifications', canActivate: [AuthGuardService], component: NotificationsComponent, data: { title: 'Notifications', breadcrumb: { title: 'Ειδοποιήσεις' } } },

  /// PATHS FOR NEW SIDE PANE FORMS GO HERE
  { path: 'new-case', component: CaseCreatePageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { title: 'New Case', paneSize: SidePaneSize.Small25, breadcrumb: { title: 'Νέα Υπόθεση' } } },
  { path: 'queries', component: QueriesPageComponent, pathMatch: 'prefix', outlet: 'rightpane', data: { title: 'Queries', paneSize: SidePaneSize.Small25, breadcrumb: { title: 'Εύρεση' } } },
  { path: 'logout', component: LogOutComponent, data: { title: 'Logout', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'Αποσύνδεση' } } },
  { path: '**', component: PageNotFoundComponent, data: { title: 'Page Not Found', shell: { fluid: true, showHeader: false, showFooter: false }, breadcrumb: { title: 'Δεν βρέθηκε η σελίδα' } } },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })], // https://stackoverflow.com/a/54098719/19162333
  exports: [RouterModule]
})
export class AppRoutingModule { }
