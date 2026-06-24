import {
  ApplicationConfig,
  importProvidersFrom,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AUTH_SETTINGS, AuthHttpInterceptor, IndiceAuthModule } from '@indice/ng-auth';

import { routes } from './app.routes';
import { settings } from './core/models/settings';
import { DEX_API_BASE_URL } from './core/services/dex-api.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    // @indice/ng-auth is NgModule-based; pull its providers into the standalone bootstrap.
    importProvidersFrom(IndiceAuthModule.forRoot()),
    { provide: AUTH_SETTINGS, useFactory: () => settings.auth_settings },
    // AuthHttpInterceptor attaches the bearer token to HttpClient calls (not fetch — see ChatStreamService).
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: DEX_API_BASE_URL, useFactory: () => settings.api_url },
  ],
};
