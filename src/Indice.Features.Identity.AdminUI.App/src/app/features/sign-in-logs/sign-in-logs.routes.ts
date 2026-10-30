import { Routes } from '@angular/router';

// Supported query params: search, succeeded (true|false), signInType, from, to (yyyy-MM-dd or ISO date), review (true),
// subject, application, plus page, pagesize, sort and dir.
export const SIGN_IN_LOG_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./sign-in-logs.component').then(m => m.SignInLogsComponent) }
];

// Entry details open in a dialog, so there are no pane routes.
export const SIGN_IN_LOG_PANE_ROUTES: Routes = [];
