import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';
import { ShellComponent } from './layout/shell.component';
import { AuthCallbackComponent, AuthRenewComponent, HomeComponent, LogoutComponent } from './pages/auth-pages';
import { StatusPageComponent } from './pages/status-page.component';
import { CAMPAIGN_PANE_ROUTES, CAMPAIGN_ROUTES } from './features/campaigns/campaigns.routes';
import { CONTACT_PANE_ROUTES, CONTACT_ROUTES } from './features/contacts/contacts.routes';
import { DISTRIBUTION_LIST_PANE_ROUTES, DISTRIBUTION_LIST_ROUTES } from './features/distribution-lists/distribution-lists.routes';
import { MEDIA_PANE_ROUTES, MEDIA_ROUTES } from './features/media-library/media-library.routes';
import { MESSAGE_TYPE_PANE_ROUTES, MESSAGE_TYPE_ROUTES } from './features/message-types/message-types.routes';
import { SETTINGS_PANE_ROUTES, SETTINGS_ROUTES } from './features/settings/settings.routes';
import { TEMPLATE_PANE_ROUTES, TEMPLATE_ROUTES } from './features/templates/templates.routes';

export const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'logout', component: LogoutComponent },
  {
    path: 'forbidden', component: StatusPageComponent,
    data: { code: '403', title: 'Status.Forbidden.Title', message: 'Status.Forbidden.Message' }
  },
  {
    path: '', component: ShellComponent, canActivate: [AuthGuardService], children: [
      {
        path: 'dashboard', data: { breadcrumb: { title: 'Breadcrumb.Home' } },
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      { path: 'campaigns', data: { breadcrumb: { title: 'Breadcrumb.Campaigns' } }, children: CAMPAIGN_ROUTES },
      { path: 'message-types', data: { breadcrumb: { title: 'Breadcrumb.MessageTypes' } }, children: MESSAGE_TYPE_ROUTES },
      { path: 'distribution-lists', data: { breadcrumb: { title: 'Breadcrumb.DistributionLists' } }, children: DISTRIBUTION_LIST_ROUTES },
      { path: 'contacts', data: { breadcrumb: { title: 'Breadcrumb.Contacts' } }, children: CONTACT_ROUTES },
      { path: 'templates', data: { breadcrumb: { title: 'Breadcrumb.Templates' } }, children: TEMPLATE_ROUTES },
      { path: 'settings', data: { breadcrumb: { title: 'Breadcrumb.Settings' } }, children: SETTINGS_ROUTES },
      {
        path: 'message-events', data: { breadcrumb: { title: 'Breadcrumb.MessageEvents' } },
        loadComponent: () => import('./features/events/message-events.component').then(m => m.MessageEventsComponent)
      },
      { path: 'media', data: { breadcrumb: { title: 'Breadcrumb.Files' } }, children: MEDIA_ROUTES }
    ]
  },

  // side pane routes (rendered in the `rightpane` slide-over)
  ...CAMPAIGN_PANE_ROUTES,
  ...CONTACT_PANE_ROUTES,
  ...DISTRIBUTION_LIST_PANE_ROUTES,
  ...MEDIA_PANE_ROUTES,
  ...MESSAGE_TYPE_PANE_ROUTES,
  ...SETTINGS_PANE_ROUTES,
  ...TEMPLATE_PANE_ROUTES,

  {
    path: '**', component: StatusPageComponent,
    data: { code: '404', title: 'Status.NotFound.Title', message: 'Status.NotFound.Message' }
  }
];
