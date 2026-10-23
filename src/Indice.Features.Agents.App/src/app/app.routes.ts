import { Routes } from '@angular/router';
import { AuthGuardService } from '@indice/ng-auth';

import { AuthCallbackComponent } from './core/auth/auth-callback.component';
import { AuthRenewComponent } from './core/auth/auth-renew.component';
import { authSettledGuard } from './core/auth/auth-settled.guard';
import { LoggedOutComponent } from './core/auth/logged-out.component';
import { EmbedShellComponent } from './core/layout/embed-shell.component';
import { ShellComponent } from './core/layout/shell.component';
import { assistantName } from './core/models/brand';
import { ChatPageComponent } from './features/chat/chat-page.component';
import { ProfilePageComponent } from './features/profile/profile-page.component';

export const routes: Routes = [
  // Public OIDC redirect endpoints (outside the shell).
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: 'logged-out', component: LoggedOutComponent },

  // The embeddable widget's panel (framed by public/widget.js): the chat page under a compact header, no rail.
  {
    path: 'embed',
    component: EmbedShellComponent,
    canActivate: [authSettledGuard],
    children: [{ path: '', component: ChatPageComponent, title: `${assistantName} — Chat` }],
  },

  // App shell (conversation rail) hosting the feature pages. Open to guests — the chat is public;
  // pages that need a signed-in user guard themselves.
  {
    path: '',
    component: ShellComponent,
    canActivate: [authSettledGuard],
    children: [
      { path: '', component: ChatPageComponent, title: `${assistantName} — Chat` },
      // Guests are sent to sign in; AuthGuardService carries the URL so they land back here.
      {
        path: 'profile',
        component: ProfilePageComponent,
        canActivate: [AuthGuardService],
        title: `${assistantName} — Profile`,
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
