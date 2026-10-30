import { Routes } from '@angular/router';

// Supported query params: search, succeeded (true|false), from, to (yyyy-MM-dd or ISO date), review (true), subject,
// subjectMode (Actor|Resource|Any), action, category, resourceType, resourceId, application, plus page, pagesize, sort and dir.
export const ACTIVITY_LOG_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./activity-logs.component').then(m => m.ActivityLogsComponent) }
];

// Entry details open in a dialog, so there are no pane routes.
export const ACTIVITY_LOG_PANE_ROUTES: Routes = [];
