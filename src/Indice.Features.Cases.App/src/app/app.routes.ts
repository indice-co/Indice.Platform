import { inject } from '@angular/core';
import { CanActivateFn, Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';
import { AuthCallbackComponent, AuthRenewComponent, HomeComponent, LogOutComponent } from './core/auth/auth-pages';
import { adminGuard } from './core/guards/admin-guard';
import { ForbiddenComponent, PageNotFoundComponent } from './features/errors/error-pages';

const authGuard: CanActivateFn = (route, state) => inject(AuthGuardService).canActivate(route, state);

/** Pages without the sidebar and top bar. */
const chromeless = { fluid: true, showHeader: false, showFooter: false };

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent, data: { title: 'Αυθεντικοποίηση', shell: chromeless, breadcrumb: { title: 'breadcrumb.authCallback' } } },
  { path: 'auth-renew', component: AuthRenewComponent, data: { title: 'Ανενέωση Αυθεντικοποίησης', shell: chromeless, breadcrumb: { title: 'breadcrumb.authRenew' } } },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { title: 'Αρχική', shell: chromeless, breadcrumb: { title: 'breadcrumb.home' } } },
  {
    path: '', canActivate: [authGuard], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        data: { title: 'Dashboard', breadcrumb: { title: 'breadcrumb.dashboard' } }
      },
      {
        path: 'cases',
        loadComponent: () => import('./features/cases/general-cases/general-cases.component').then(m => m.GeneralCasesComponent),
        data: { title: 'Υποθέσεις', breadcrumb: { title: 'breadcrumb.cases' } }
      },
      {
        path: 'case/by-type/:caseTypeCode', pathMatch: 'prefix',
        loadComponent: () => import('./features/cases/case-type-specific-cases/case-type-specific-cases.component').then(m => m.CaseTypeSpecificCasesComponent),
        data: { title: 'Υποθέσεις Συγκεκριμένου Τύπου', breadcrumb: { title: 'breadcrumb.casesByType' } }
      },
      {
        path: 'cases/:caseId', pathMatch: 'prefix', data: { title: 'Υπόθεση', breadcrumb: { title: 'breadcrumb.case' } },
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          {
            path: 'details',
            loadComponent: () => import('./features/cases/case-detail-page/case-detail-page.component').then(m => m.CaseDetailPageComponent),
            data: { title: 'Λεπτομέρειες Υπόθεσης', breadcrumb: { title: 'breadcrumb.caseDetails' } }
          }
        ]
      },
      {
        path: 'case-types', pathMatch: 'prefix', canActivate: [adminGuard], data: { title: 'Τύποι Υπόθεσης', breadcrumb: { title: 'breadcrumb.caseTypes' } },
        children: [
          {
            path: '',
            loadComponent: () => import('./features/case-types/case-types.component').then(m => m.CaseTypesComponent),
            data: { title: 'Τύποι Υπόθεσης', breadcrumb: { title: 'breadcrumb.caseTypes' } }
          },
          {
            path: 'create', pathMatch: 'full',
            loadComponent: () => import('./features/case-types/case-type-create/case-type-create.component').then(m => m.CaseTypeCreateComponent),
            data: { title: 'Δημιουργία Υπόθεσης', breadcrumb: { title: 'breadcrumb.createCaseType' } }
          },
          {
            path: ':caseTypeId/edit', pathMatch: 'full',
            loadComponent: () => import('./features/case-types/case-type-edit/case-type-edit.component').then(m => m.CaseTypeEditComponent),
            data: { title: 'Επεξεργασία Τύπου Υπόθεσης', breadcrumb: { title: 'breadcrumb.editCaseType' } }
          }
        ]
      },
      {
        path: 'checkpoint-types', pathMatch: 'prefix', canActivate: [adminGuard], data: { title: 'Checkpoint Types', breadcrumb: { title: 'breadcrumb.checkpointTypes' } },
        children: [
          {
            path: '',
            loadComponent: () => import('./features/checkpoint-types/checkpoint-types.component').then(m => m.CheckpointTypesComponent),
            data: { title: 'Checkpoint Types', breadcrumb: { title: 'breadcrumb.checkpointTypes' } }
          },
          {
            path: 'create', pathMatch: 'full',
            loadComponent: () => import('./features/checkpoint-types/checkpoint-type-edit/checkpoint-type-edit.component').then(m => m.CheckpointTypeEditComponent),
            data: { title: 'Create Checkpoint Type', breadcrumb: { title: 'breadcrumb.createCheckpointType' } }
          },
          {
            path: ':checkpointTypeId/edit', pathMatch: 'full',
            loadComponent: () => import('./features/checkpoint-types/checkpoint-type-edit/checkpoint-type-edit.component').then(m => m.CheckpointTypeEditComponent),
            data: { title: 'Edit Checkpoint Type', breadcrumb: { title: 'breadcrumb.editCheckpointType' } }
          }
        ]
      },
      {
        path: 'access-rules', pathMatch: 'prefix', canActivate: [adminGuard], data: { title: 'Access Rules', breadcrumb: { title: 'breadcrumb.accessRules' } },
        children: [
          {
            path: '',
            loadComponent: () => import('./features/access-rules/access-rules.component').then(m => m.AccessRulesComponent),
            data: { title: 'Access Rules', breadcrumb: { title: 'breadcrumb.accessRules' } }
          },
          {
            path: 'create', pathMatch: 'full',
            loadComponent: () => import('./features/access-rules/access-rule-create/access-rule-create.component').then(m => m.AccessRuleCreateComponent),
            data: { title: 'Create Access Rule', breadcrumb: { title: 'breadcrumb.createAccessRule' } }
          }
        ]
      }
    ]
  },
  {
    path: 'notifications', canActivate: [authGuard],
    loadComponent: () => import('./features/notifications/notifications.component').then(m => m.NotificationsComponent),
    data: { title: 'Notifications', breadcrumb: { title: 'breadcrumb.notifications' } }
  },

  // Side pane pages (the `rightpane` outlet).
  {
    path: 'new-case', outlet: 'rightpane', pathMatch: 'prefix',
    loadComponent: () => import('./features/cases/case-create-page/case-create-page.component').then(m => m.CaseCreatePageComponent),
    data: { title: 'New Case', breadcrumb: { title: 'breadcrumb.newCase' } }
  },
  {
    path: 'queries', outlet: 'rightpane', pathMatch: 'prefix',
    loadComponent: () => import('./features/cases/queries-page/queries-page.component').then(m => m.QueriesPageComponent),
    data: { title: 'Queries', breadcrumb: { title: 'breadcrumb.queries' } }
  },

  { path: 'logout', component: LogOutComponent, data: { title: 'Logout', shell: chromeless, breadcrumb: { title: 'breadcrumb.logout' } } },
  { path: 'forbidden', component: ForbiddenComponent, data: { title: 'Forbidden', shell: chromeless, breadcrumb: { title: 'breadcrumb.forbidden' } } },
  { path: '**', component: PageNotFoundComponent, data: { title: 'Page Not Found', shell: chromeless, breadcrumb: { title: 'breadcrumb.pageNotFound' } } }
];
