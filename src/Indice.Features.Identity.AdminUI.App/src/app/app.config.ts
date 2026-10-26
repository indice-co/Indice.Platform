import { ApplicationConfig, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeGreek from '@angular/common/locales/el';
import localeSpanish from '@angular/common/locales/es';
import localeItaly from '@angular/common/locales/it';
import localeJapanese from '@angular/common/locales/ja';
import localeEn from '@angular/common/locales/en-GB';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideLucideConfig } from '@lucide/angular';
import { progressInterceptor } from 'ngx-progressbar/http';
import { settings } from './core/models/settings';
import { problemDetailsInterceptor } from './core/http/problem-details.interceptor';
import { AuthInterceptor } from './core/services/auth.interceptor';
import { IDENTITY_API_BASE_URL } from './core/services/identity-api.service';
import { routes } from './app.routes';

registerLocaleData(localeGreek);
registerLocaleData(localeSpanish);
registerLocaleData(localeItaly);
registerLocaleData(localeJapanese);
registerLocaleData(localeEn);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withXhr(), withInterceptors([progressInterceptor, problemDetailsInterceptor]), withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: IDENTITY_API_BASE_URL, useFactory: () => settings.api_url },
    { provide: LOCALE_ID, useValue: settings.culture || 'en-GB' },
    provideLucideConfig({ strokeWidth: 1.75 })
  ]
};
