import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';
import { adminGuard } from './core/guards/admin-guard';
import { ShellComponent } from './layout/shell.component';
import { AuthCallbackComponent, AuthRenewComponent, HomeComponent, LogoutComponent } from './pages/auth-pages';
import { StatusPageComponent } from './pages/status-page.component';
import { SIDE_PANE_OUTLET } from './ui/side-pane/side-pane';

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent, data: { breadcrumb: { title: 'breadcrumb.authCallback' } } },
  { path: 'auth-renew', component: AuthRenewComponent, data: { breadcrumb: { title: 'breadcrumb.authRenew' } } },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, data: { breadcrumb: { title: 'breadcrumb.home' } } },
  { path: 'logout', component: LogoutComponent, data: { breadcrumb: { title: 'breadcrumb.logout' } } },
  {
    path: 'forbidden', component: StatusPageComponent,
    data: { code: '403', title: 'status.forbidden.title', message: 'status.forbidden.message', breadcrumb: { title: 'breadcrumb.forbidden' } }
  },
  {
    path: '', component: ShellComponent, canActivate: [AuthGuardService], children: [
      {
        path: 'dashboard', data: { breadcrumb: { title: 'breadcrumb.dashboard' } },
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'cases', data: { breadcrumb: { title: 'breadcrumb.cases' } },
        loadComponent: () => import('./features/cases/general-cases/general-cases.component').then(m => m.GeneralCasesComponent)
      },
      {
        path: 'case/by-type/:caseTypeCode', data: { breadcrumb: { title: 'breadcrumb.casesByType' } },
        loadComponent: () => import('./features/cases/case-type-specific-cases/case-type-specific-cases.component').then(m => m.CaseTypeSpecificCasesComponent)
      },
      {
        path: 'cases/:caseId', data: { breadcrumb: { title: 'breadcrumb.case' } },
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          {
            path: 'details', data: { breadcrumb: { title: 'breadcrumb.caseDetails' } },
            loadComponent: () => import('./features/cases/case-detail-page/case-detail-page.component').then(m => m.CaseDetailPageComponent)
          }
        ]
      },
      {
        path: 'case-types', canActivate: [adminGuard], data: { breadcrumb: { title: 'breadcrumb.caseTypes' } },
        children: [
          { path: '', loadComponent: () => import('./features/case-types/case-types.component').then(m => m.CaseTypesComponent) },
          {
            path: 'create', data: { breadcrumb: { title: 'breadcrumb.createCaseType' } },
            loadComponent: () => import('./features/case-types/case-type-create/case-type-create.component').then(m => m.CaseTypeCreateComponent)
          },
          {
            path: ':caseTypeId/edit', data: { breadcrumb: { title: 'breadcrumb.editCaseType' } },
            loadComponent: () => import('./features/case-types/case-type-edit/case-type-edit.component').then(m => m.CaseTypeEditComponent)
          }
        ]
      },
      {
        path: 'checkpoint-types', canActivate: [adminGuard], data: { breadcrumb: { title: 'breadcrumb.checkpointTypes' } },
        children: [
          { path: '', loadComponent: () => import('./features/checkpoint-types/checkpoint-types.component').then(m => m.CheckpointTypesComponent) },
          {
            path: 'create', data: { breadcrumb: { title: 'breadcrumb.createCheckpointType' } },
            loadComponent: () => import('./features/checkpoint-types/checkpoint-type-edit/checkpoint-type-edit.component').then(m => m.CheckpointTypeEditComponent)
          },
          {
            path: ':checkpointTypeId/edit', data: { breadcrumb: { title: 'breadcrumb.editCheckpointType' } },
            loadComponent: () => import('./features/checkpoint-types/checkpoint-type-edit/checkpoint-type-edit.component').then(m => m.CheckpointTypeEditComponent)
          }
        ]
      },
      {
        path: 'access-rules', canActivate: [adminGuard], data: { breadcrumb: { title: 'breadcrumb.accessRules' } },
        children: [
          { path: '', loadComponent: () => import('./features/access-rules/access-rules.component').then(m => m.AccessRulesComponent) },
          {
            path: 'create', data: { breadcrumb: { title: 'breadcrumb.createAccessRule' } },
            loadComponent: () => import('./features/access-rules/access-rule-create/access-rule-create.component').then(m => m.AccessRuleCreateComponent)
          }
        ]
      },
      {
        path: 'notifications', data: { breadcrumb: { title: 'breadcrumb.notifications' } },
        loadComponent: () => import('./features/notifications/notifications.component').then(m => m.NotificationsComponent)
      }
    ]
  },

  // side pane routes (rendered in the `rightpane` slide-over)
  {
    path: 'new-case', outlet: SIDE_PANE_OUTLET, data: { paneSize: 'md', breadcrumb: { title: 'breadcrumb.newCase' } },
    loadComponent: () => import('./features/cases/case-create-page/case-create-page.component').then(m => m.CaseCreatePageComponent)
  },
  {
    path: 'queries', outlet: SIDE_PANE_OUTLET, data: { paneSize: 'sm', breadcrumb: { title: 'breadcrumb.queries' } },
    loadComponent: () => import('./features/cases/queries-page/queries-page.component').then(m => m.QueriesPageComponent)
  },
  // administrators only (Administer access, enforced by the API); the details page shows their links to administrators
  {
    path: 'case-relations', outlet: SIDE_PANE_OUTLET, data: { paneSize: 'md', breadcrumb: { title: 'breadcrumb.caseRelations' } },
    loadComponent: () => import('./features/cases/case-relations-pane/case-relations-pane.component').then(m => m.CaseRelationsPaneComponent)
  },
  {
    path: 'case-group', outlet: SIDE_PANE_OUTLET, data: { paneSize: 'sm', breadcrumb: { title: 'breadcrumb.caseGroup' } },
    loadComponent: () => import('./features/cases/case-group-pane/case-group-pane.component').then(m => m.CaseGroupPaneComponent)
  },

  {
    path: '**', component: StatusPageComponent,
    data: { code: '404', title: 'status.notFound.title', message: 'status.notFound.message', breadcrumb: { title: 'breadcrumb.pageNotFound' } }
  }
];
