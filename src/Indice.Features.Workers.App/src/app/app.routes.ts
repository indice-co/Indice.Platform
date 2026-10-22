import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';

import { AuthCallbackComponent } from './core/auth/auth-callback.component';
import { AuthRenewComponent } from './core/auth/auth-renew.component';
import { LoggedOutComponent } from './core/auth/logged-out.component';
import { ShellComponent } from './core/layout/shell.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page.component';
import { MessageDetailPageComponent } from './features/queues/message-detail-page.component';
import { QueueDetailPageComponent } from './features/queues/queue-detail-page.component';
import { QueuesPageComponent } from './features/queues/queues-page.component';
import { ScheduledTaskDetailPageComponent } from './features/scheduled/scheduled-task-detail-page.component';
import { ScheduledTasksPageComponent } from './features/scheduled/scheduled-tasks-page.component';
import { ServersPageComponent } from './features/servers/servers-page.component';

export const routes: Routes = [
  // Public OIDC redirect endpoints (outside the shell).
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: 'logged-out', component: LoggedOutComponent },

  // App shell hosting the dashboard pages. Signed-in users only — there is no guest mode.
  {
    path: '',
    component: ShellComponent,
    canActivate: [AuthGuardService],
    children: [
      { path: '', component: DashboardPageComponent, title: 'Workers — Dashboard' },
      { path: 'queues', component: QueuesPageComponent, title: 'Workers — Queues' },
      // ?status=Enqueued|Scheduled|Retrying|Poison|Dequeued
      { path: 'queues/:name', component: QueueDetailPageComponent, title: 'Workers — Queue' },
      { path: 'messages/:id', component: MessageDetailPageComponent, title: 'Workers — Message' },
      {
        path: 'scheduled',
        component: ScheduledTasksPageComponent,
        title: 'Workers — Scheduled tasks',
      },
      {
        path: 'scheduled/:id',
        component: ScheduledTaskDetailPageComponent,
        title: 'Workers — Scheduled task',
      },
      { path: 'servers', component: ServersPageComponent, title: 'Workers — Servers & locks' },
    ],
  },

  { path: '**', redirectTo: '' },
];
