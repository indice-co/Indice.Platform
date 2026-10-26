import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const ROLE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./roles.component').then(m => m.RolesComponent) },
  // the "Create" menu and older links land on the list with the create pane open
  { path: 'add', redirectTo: '/roles(rightpane:role-add)' }
];

export const ROLE_PANE_ROUTES: Routes = [
  paneRoute('role-add', () => import('./role-edit.component').then(m => m.RoleEditComponent), 'sm'),
  paneRoute('role/:id', () => import('./role-edit.component').then(m => m.RoleEditComponent), 'sm')
];
