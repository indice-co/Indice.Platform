import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const CLAIM_TYPE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./claim-types.component').then(m => m.ClaimTypesComponent) },
  // the "Create" menu and older links land on the list with the create pane open
  { path: 'add', redirectTo: '/claim-types(rightpane:claim-type-add)' }
];

export const CLAIM_TYPE_PANE_ROUTES: Routes = [
  paneRoute('claim-type-add', () => import('./claim-type-edit.component').then(m => m.ClaimTypeEditComponent)),
  paneRoute('claim-type/:id', () => import('./claim-type-edit.component').then(m => m.ClaimTypeEditComponent))
];
