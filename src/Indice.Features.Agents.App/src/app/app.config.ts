import {
  ApplicationConfig,
  importProvidersFrom,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';
import { provideMarkdown } from 'ngx-markdown';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { settings } from './core/models/settings';
import { DEX_API_BASE_URL } from './core/services/dex-api.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    // authInterceptor attaches the user or guest bearer token to HttpClient calls (not fetch — see
    // ChatStreamService). It replaces ng-auth's AuthHttpInterceptor; its doc comment says why.
    provideHttpClient(withInterceptors([authInterceptor])),
    // @indice/ng-auth is NgModule-based; pull its providers into the standalone bootstrap.
    importProvidersFrom(IndiceAuthModule.forRoot()),
    { provide: AUTH_SETTINGS, useFactory: () => settings.auth_settings },
    { provide: DEX_API_BASE_URL, useFactory: () => settings.api_url },
    // Provide markdown service for rendering markdown content in chat messages.
    provideMarkdown(),
  ],
};
