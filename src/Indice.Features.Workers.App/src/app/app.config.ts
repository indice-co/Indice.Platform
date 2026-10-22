import {
  ApplicationConfig,
  importProvidersFrom,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { settings } from './core/models/settings';
import { WORKERS_API_BASE_URL } from './core/services/workers-api.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    // Route and query params bind straight to page `input()`s.
    provideRouter(routes, withComponentInputBinding()),
    // authInterceptor attaches the user's bearer token to HttpClient calls. It replaces ng-auth's
    // AuthHttpInterceptor; its doc comment says why.
    provideHttpClient(withInterceptors([authInterceptor])),
    // @indice/ng-auth is NgModule-based; pull its providers into the standalone bootstrap.
    importProvidersFrom(IndiceAuthModule.forRoot()),
    { provide: AUTH_SETTINGS, useFactory: () => settings.auth_settings },
    { provide: WORKERS_API_BASE_URL, useFactory: () => settings.api_url },
  ],
};
