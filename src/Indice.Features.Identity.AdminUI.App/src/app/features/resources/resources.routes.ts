import { Routes } from '@angular/router';
import { permissionGuard } from 'src/app/core/services/auth.guards';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

const list = () => import('./resource-list.component').then(m => m.ResourceListComponent);

export const RESOURCE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'identity' },
  {
    path: 'add', data: { breadcrumb: 'Routes.NewResource' }, canActivate: [permissionGuard('clientsWriter')],
    loadComponent: () => import('./add/resource-add.component').then(m => m.ResourceAddComponent)
  },
  {
    path: 'identity', data: { breadcrumb: 'Routes.IdentityResources' }, children: [
      { path: '', pathMatch: 'full', loadComponent: list, data: { kind: 'identity' } },
      {
        path: ':id', loadComponent: () => import('./identity/identity-resource-edit.component').then(m => m.IdentityResourceEditComponent),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          { path: 'details', loadComponent: () => import('./identity/identity-resource-details.component').then(m => m.IdentityResourceDetailsComponent) },
          { path: 'claims', loadComponent: () => import('./identity/identity-resource-claims.component').then(m => m.IdentityResourceClaimsComponent) }
        ]
      }
    ]
  },
  {
    path: 'api', data: { breadcrumb: 'Routes.ApiResources' }, children: [
      { path: '', pathMatch: 'full', loadComponent: list, data: { kind: 'api' } },
      {
        path: ':id', loadComponent: () => import('./api/api-resource-edit.component').then(m => m.ApiResourceEditComponent),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'details' },
          { path: 'details', loadComponent: () => import('./api/api-resource-details.component').then(m => m.ApiResourceDetailsComponent) },
          { path: 'scopes', loadComponent: () => import('./api/api-resource-scopes.component').then(m => m.ApiResourceScopesComponent) },
          { path: 'claims', loadComponent: () => import('./api/api-resource-claims.component').then(m => m.ApiResourceClaimsComponent) },
          { path: 'secrets', loadComponent: () => import('./api/api-resource-secrets.component').then(m => m.ApiResourceSecretsComponent) }
        ]
      }
    ]
  },
  {
    path: 'scopes', data: { breadcrumb: 'Routes.ApiScopes' }, children: [
      { path: '', pathMatch: 'full', loadComponent: list, data: { kind: 'scope' } },
      { path: ':name', loadComponent: () => import('./scopes/scope-edit.component').then(m => m.ScopeEditComponent) }
    ]
  }
];

export const RESOURCE_PANE_ROUTES: Routes = [
  paneRoute('resource-api-scope-add/:resourceId', () => import('./api/api-resource-scope-add.component').then(m => m.ApiResourceScopeAddComponent))
];
