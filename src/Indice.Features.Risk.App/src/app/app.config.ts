import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { AUTH_SETTINGS, AuthHttpInterceptor, provideIndiceAuth } from '@indice/ng-auth';
import { provideLucideConfig } from '@lucide/angular';
import { settings } from './core/models/settings';
import { problemDetailsInterceptor } from './core/http/problem-details.interceptor';
import { RISK_API_BASE_URL } from './core/services/risk-api.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withXhr(), withInterceptors([problemDetailsInterceptor]), withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    provideIndiceAuth(),
    { provide: AUTH_SETTINGS, useFactory: () => settings.auth_settings },
    { provide: RISK_API_BASE_URL, useFactory: () => settings.api_url },
    provideLucideConfig({ strokeWidth: 1.75 })
  ]
};
