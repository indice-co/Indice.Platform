import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';

import { AuthCallbackComponent } from './core/auth/auth-callback.component';
import { AuthRenewComponent } from './core/auth/auth-renew.component';
import { authSettledGuard } from './core/auth/auth-settled.guard';
import { LoggedOutComponent } from './core/auth/logged-out.component';
import { ShellComponent } from './core/layout/shell.component';
import { ChatPageComponent } from './features/chat/chat-page.component';
import { ProfilePageComponent } from './features/profile/profile-page.component';

export const routes: Routes = [
  // Public OIDC redirect endpoints (outside the shell).
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: 'logged-out', component: LoggedOutComponent },

  // App shell (conversation rail) hosting the feature pages. Open to guests — the chat is public;
  // pages that need a signed-in user guard themselves.
  {
    path: '',
    component: ShellComponent,
    canActivate: [authSettledGuard],
    children: [
      { path: '', component: ChatPageComponent, title: 'Dex — Chat' },
      // Guests are sent to sign in; AuthGuardService carries the URL so they land back here.
      {
        path: 'profile',
        component: ProfilePageComponent,
        canActivate: [AuthGuardService],
        title: 'Dex — Profile',
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
