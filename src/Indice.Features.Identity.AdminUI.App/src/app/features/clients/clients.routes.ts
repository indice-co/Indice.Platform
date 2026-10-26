import { Routes } from '@angular/router';
import { permissionGuard } from 'src/app/core/services/auth.guards';

export const CLIENT_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./clients.component').then(m => m.ClientsComponent) },
  {
    path: 'add', data: { breadcrumb: 'New client' }, canActivate: [permissionGuard('clientsWriter')],
    loadComponent: () => import('./add/client-add.component').then(m => m.ClientAddComponent)
  },
  {
    path: ':id', data: { breadcrumb: 'Client' },
    loadComponent: () => import('./edit/client-edit.component').then(m => m.ClientEditComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'details' },
      { path: 'details', loadComponent: () => import('./edit/client-details.component').then(m => m.ClientDetailsComponent) },
      { path: 'urls', loadComponent: () => import('./edit/client-urls.component').then(m => m.ClientUrlsComponent) },
      { path: 'secrets', loadComponent: () => import('./edit/client-secrets.component').then(m => m.ClientSecretsComponent) },
      { path: 'resources', loadComponent: () => import('./edit/client-resources.component').then(m => m.ClientResourcesComponent) },
      { path: 'tokens', loadComponent: () => import('./edit/client-tokens.component').then(m => m.ClientTokensComponent) },
      { path: 'claims', loadComponent: () => import('./edit/client-claims.component').then(m => m.ClientClaimsComponent) },
      { path: 'grant-types', loadComponent: () => import('./edit/client-grant-types.component').then(m => m.ClientGrantTypesComponent) },
      { path: 'theme', loadComponent: () => import('./edit/client-theme.component').then(m => m.ClientThemeComponent) },
      // tab urls of the previous UI
      { path: 'resources/:kind', redirectTo: 'resources' },
      { path: 'advanced', redirectTo: 'tokens' },
      { path: 'advanced/:tab', redirectTo: ':tab' }
    ]
  }
];

export const CLIENT_PANE_ROUTES: Routes = [];
