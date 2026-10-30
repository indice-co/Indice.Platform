import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';
import { ShellComponent } from './layout/shell.component';
import { AuthCallbackComponent, AuthRenewComponent, HomeComponent, LogoutComponent } from './pages/auth-pages';
import { StatusPageComponent } from './pages/status-page.component';
import { paneRoute } from './ui/side-pane/side-pane';

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'logout', component: LogoutComponent },
  {
    path: 'logged-out', component: StatusPageComponent,
    data: { code: 'Bye', title: 'You have signed out', message: 'Your session has ended. Sign in again to keep working.', action: 'Sign in' }
  },
  {
    path: 'forbidden', component: StatusPageComponent,
    data: { code: '403', title: 'Access denied', message: 'Your account is not allowed to view this page.' }
  },
  {
    path: '', component: ShellComponent, canActivate: [AuthGuardService], children: [
      {
        path: 'risk-events',
        loadComponent: () => import('./features/risk-events/risk-events.component').then(m => m.RiskEventsComponent)
      },
      {
        path: 'risk-results',
        loadComponent: () => import('./features/risk-results/risk-results.component').then(m => m.RiskResultsComponent)
      },
      {
        path: 'rules',
        loadComponent: () => import('./features/rules/rules.component').then(m => m.RulesComponent)
      }
    ]
  },

  // side pane routes (rendered in the `rightpane` slide-over)
  paneRoute('event/:id', () => import('./features/risk-events/risk-event-pane.component').then(m => m.RiskEventPaneComponent), 'lg'),
  paneRoute('result/:id', () => import('./features/risk-results/risk-result-pane.component').then(m => m.RiskResultPaneComponent), 'lg'),
  paneRoute('rule/:ruleName', () => import('./features/rules/rule-options-pane.component').then(m => m.RuleOptionsPaneComponent)),

  {
    path: '**', component: StatusPageComponent,
    data: { code: '404', title: 'Page not found', message: 'The page you are looking for does not exist or has moved.' }
  }
];
