import { Routes } from '@angular/router';
import { Features } from 'src/app/core/models/features';
import { featureActivateGuard, permissionGuard } from 'src/app/core/services/auth.guards';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const USER_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./users.component').then(m => m.UsersComponent) },
  {
    path: 'add', data: { breadcrumb: 'New user' }, canActivate: [permissionGuard('usersWriter')],
    loadComponent: () => import('./add/user-add.component').then(m => m.UserAddComponent)
  },
  {
    path: ':id', data: { breadcrumb: 'User' }, loadComponent: () => import('./edit/user-edit.component').then(m => m.UserEditComponent),
    children: [
      { path: '', redirectTo: 'details', pathMatch: 'full' },
      { path: 'details', loadComponent: () => import('./edit/user-details.component').then(m => m.UserDetailsComponent) },
      { path: 'roles', data: { breadcrumb: 'Roles' }, loadComponent: () => import('./edit/user-roles.component').then(m => m.UserRolesComponent) },
      { path: 'claims', data: { breadcrumb: 'Claims' }, loadComponent: () => import('./edit/user-claims.component').then(m => m.UserClaimsComponent) },
      // links of the previous UI
      { path: 'additional-details', redirectTo: 'claims' },
      {
        path: 'applications', data: { breadcrumb: 'Applications' },
        loadComponent: () => import('./edit/user-applications.component').then(m => m.UserApplicationsComponent)
      },
      {
        path: 'external-logins', data: { breadcrumb: 'External logins' },
        loadComponent: () => import('./edit/user-logins.component').then(m => m.UserLoginsComponent)
      },
      { path: 'devices', data: { breadcrumb: 'Devices' }, loadComponent: () => import('./edit/user-devices.component').then(m => m.UserDevicesComponent) },
      { path: 'sessions', data: { breadcrumb: 'Sessions' }, loadComponent: () => import('./edit/user-sessions.component').then(m => m.UserSessionsComponent) },
      {
        path: 'sign-in-logs', data: { breadcrumb: 'Sign-in logs' }, canActivate: [featureActivateGuard(Features.SignInLogs)],
        loadComponent: () => import('./edit/user-logs.component').then(m => m.UserSignInLogsComponent)
      },
      {
        path: 'activity-logs', data: { breadcrumb: 'Activity logs' }, canActivate: [featureActivateGuard(Features.ActivityLogs)],
        loadComponent: () => import('./edit/user-logs.component').then(m => m.UserActivityLogsComponent)
      }
    ]
  }
];

export const USER_PANE_ROUTES: Routes = [
  paneRoute('user-claim/:userId', () => import('./panes/user-claim-edit.component').then(m => m.UserClaimEditComponent), 'sm'),
  paneRoute('user-claim/:userId/:claimId', () => import('./panes/user-claim-edit.component').then(m => m.UserClaimEditComponent), 'sm'),
  paneRoute('user-password/:userId', () => import('./panes/user-password.component').then(m => m.UserPasswordComponent), 'sm')
];
