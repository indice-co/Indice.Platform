import { ApplicationConfig, LOCALE_ID, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeGreek from '@angular/common/locales/el';
import localeEn from '@angular/common/locales/en-GB';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { provideLucideConfig } from '@lucide/angular';
import { progressInterceptor } from 'ngx-progressbar/http';
import { settings } from './core/models/settings';
import { problemDetailsInterceptor } from './core/http/problem-details.interceptor';
import { acceptLanguageInterceptor } from './core/i18n/accept-language.interceptor';
import { AppLanguagesService, DEFAULT_LANGUAGE, initialLanguage, localeOf } from './core/i18n/app-languages.service';
import { AuthInterceptor } from './core/services/auth.interceptor';
import { IDENTITY_API_BASE_URL } from './core/services/identity-api.service';
import { routes } from './app.routes';

registerLocaleData(localeGreek);
registerLocaleData(localeEn);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withXhr(), withInterceptors([progressInterceptor, acceptLanguageInterceptor, problemDetailsInterceptor]), withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: IDENTITY_API_BASE_URL, useFactory: () => settings.api_url },
    { provide: LOCALE_ID, useFactory: () => localeOf(initialLanguage()) },
    // translations are the server side resx resources exposed by the identity server's translation graph endpoint
    provideTranslateService({
        loader: provideTranslateHttpLoader({ prefix: `${settings.api_url}/api/iam-i18n.`, suffix: '.json', useHttpBackend: true }),
      fallbackLang: DEFAULT_LANGUAGE
    }),
    provideAppInitializer(() => inject(AppLanguagesService).init()),
    provideLucideConfig({ strokeWidth: 1.75 })
  ]
};
