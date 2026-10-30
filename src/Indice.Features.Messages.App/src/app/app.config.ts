import { ApplicationConfig, LOCALE_ID, Provider, provideZoneChangeDetection } from '@angular/core';
import { DatePipe, JsonPipe, registerLocaleData } from '@angular/common';
import localeGreek from '@angular/common/locales/el';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { AUTH_SETTINGS, AuthHttpInterceptor, TENANT_PREFIX_URL, TenantHeaderInterceptor, TenantService, provideIndiceAuth } from '@indice/ng-auth';
import { provideLucideConfig } from '@lucide/angular';
import { HIGHLIGHT_OPTIONS } from 'ngx-highlightjs';
import { progressInterceptor } from 'ngx-progressbar/http';
import { settings } from './core/models/settings';
import { problemDetailsInterceptor } from './core/http/problem-details.interceptor';
import { MEDIA_API_BASE_URL } from './core/services/media-api.service';
import { MESSAGES_API_BASE_URL } from './core/services/messages-api.service';
import { routes } from './app.routes';

registerLocaleData(localeGreek);

const tenantProviders: Provider[] = settings.tenantId
  ? [{ provide: HTTP_INTERCEPTORS, useClass: TenantHeaderInterceptor, multi: true, deps: [MESSAGES_API_BASE_URL, TenantService] }]
  : [];

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withXhr(), withInterceptors([progressInterceptor, problemDetailsInterceptor]), withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    ...tenantProviders,
    provideIndiceAuth(),
    { provide: AUTH_SETTINGS, useFactory: () => settings.auth_settings },
    { provide: MESSAGES_API_BASE_URL, useFactory: () => settings.api_url },
    { provide: MEDIA_API_BASE_URL, useFactory: () => settings.api_url },
    { provide: TENANT_PREFIX_URL, useExisting: MESSAGES_API_BASE_URL },
    { provide: LOCALE_ID, useValue: 'el-GR' },
    DatePipe,
    JsonPipe,
    {
      provide: HIGHLIGHT_OPTIONS,
      useValue: {
        fullLibraryLoader: () => import('highlight.js'),
        lineNumbers: true
      }
    },
    provideTranslateService({
      loader: provideTranslateHttpLoader({ prefix: `${settings.api_url}/msg-i18n.`, useHttpBackend: true }),
      fallbackLang: 'en'
    }),
    provideLucideConfig({ strokeWidth: 1.75 })
  ]
};
