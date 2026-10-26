import { Routes } from '@angular/router';
import { Features } from './core/models/features';
import { authGuard, featureGuard, permissionGuard } from './core/services/auth.guards';
import { ShellComponent } from './layout/shell.component';
import { AuthCallbackComponent, AuthRenewComponent, HomeComponent, LogoutComponent, StatusPageComponent } from './pages/auth-pages';
import { ACTIVITY_LOG_PANE_ROUTES, ACTIVITY_LOG_ROUTES } from './features/activity-logs/activity-logs.routes';
import { CLAIM_TYPE_PANE_ROUTES, CLAIM_TYPE_ROUTES } from './features/claim-types/claim-types.routes';
import { CLIENT_PANE_ROUTES, CLIENT_ROUTES } from './features/clients/clients.routes';
import { RESOURCE_PANE_ROUTES, RESOURCE_ROUTES } from './features/resources/resources.routes';
import { ROLE_PANE_ROUTES, ROLE_ROUTES } from './features/roles/roles.routes';
import { SETTING_PANE_ROUTES, SETTING_ROUTES } from './features/settings/settings.routes';
import { SIGN_IN_LOG_PANE_ROUTES, SIGN_IN_LOG_ROUTES } from './features/sign-in-logs/sign-in-logs.routes';
import { USER_PANE_ROUTES, USER_ROUTES } from './features/users/users.routes';

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'logout', component: LogoutComponent },
  {
    path: 'forbidden', component: StatusPageComponent,
    data: { code: '403', title: 'Access denied', message: 'Your account is not allowed to use this part of the Admin UI. Ask an administrator for access.' }
  },
  // links of the previous UI lived under /app
  { path: 'app', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: '', component: ShellComponent, canActivate: [authGuard], children: [
      {
        path: 'dashboard', data: { breadcrumb: 'Dashboard' }, canActivate: [permissionGuard('usersReader', 'clientsReader')],
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      { path: 'users', data: { breadcrumb: 'Users' }, canActivate: [permissionGuard('usersReader')], children: USER_ROUTES },
      { path: 'roles', data: { breadcrumb: 'Roles' }, canActivate: [permissionGuard('usersReader')], children: ROLE_ROUTES },
      {
        path: 'claim-types', data: { breadcrumb: 'Claim types' }, canActivate: [permissionGuard('usersReader', 'clientsReader')],
        children: CLAIM_TYPE_ROUTES
      },
      { path: 'clients', data: { breadcrumb: 'Clients' }, canActivate: [permissionGuard('clientsReader')], children: CLIENT_ROUTES },
      { path: 'resources', canActivate: [permissionGuard('clientsReader')], children: RESOURCE_ROUTES },
      { path: 'settings', data: { breadcrumb: 'App settings' }, canActivate: [permissionGuard('admin')], children: SETTING_ROUTES },
      {
        path: 'sign-in-logs', data: { breadcrumb: 'Sign-in logs' }, canMatch: [featureGuard(Features.SignInLogs)],
        canActivate: [permissionGuard('admin')], children: SIGN_IN_LOG_ROUTES
      },
      {
        path: 'activity-logs', data: { breadcrumb: 'Activity logs' }, canMatch: [featureGuard(Features.ActivityLogs)],
        canActivate: [permissionGuard('admin')], children: ACTIVITY_LOG_ROUTES
      }
    ]
  },

  // side pane routes (rendered in the `rightpane` slide-over)
  ...USER_PANE_ROUTES,
  ...ROLE_PANE_ROUTES,
  ...CLAIM_TYPE_PANE_ROUTES,
  ...CLIENT_PANE_ROUTES,
  ...RESOURCE_PANE_ROUTES,
  ...SETTING_PANE_ROUTES,
  ...SIGN_IN_LOG_PANE_ROUTES,
  ...ACTIVITY_LOG_PANE_ROUTES,

  { path: '**', component: StatusPageComponent, data: { code: '404' } }
];
