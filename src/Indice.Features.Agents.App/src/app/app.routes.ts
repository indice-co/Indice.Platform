import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';

import { AuthCallbackComponent } from './core/auth/auth-callback.component';
import { AuthRenewComponent } from './core/auth/auth-renew.component';
import { LoggedOutComponent } from './core/auth/logged-out.component';
import { ShellComponent } from './core/layout/shell.component';
import { ChatPageComponent } from './features/chat/chat-page.component';
import { ProfilePageComponent } from './features/profile/profile-page.component';

export const routes: Routes = [
  // Public OIDC redirect endpoints (outside the shell).
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: 'logged-out', component: LoggedOutComponent },

  // Authenticated app shell (top nav bar) hosting the feature pages.
  {
    path: '',
    component: ShellComponent,
    canActivate: [AuthGuardService],
    children: [
      { path: '', component: ChatPageComponent, title: 'Dex — Chat' },
      { path: 'profile', component: ProfilePageComponent, title: 'Dex — Profile' },
    ],
  },

  // Unlisted rendering harness for the chat content parts. Outside the shell so it needs no login, lazy so it costs
  // the main bundle nothing, and it calls no API — see features/dev/part-gallery.component.ts.
  {
    path: 'dev/parts',
    loadComponent: () => import('./features/dev/part-gallery.component').then((m) => m.PartGalleryComponent),
    title: 'Dex — Part gallery',
  },
  { path: '**', redirectTo: '' },
];
