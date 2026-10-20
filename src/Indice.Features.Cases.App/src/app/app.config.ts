import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { AUTH_SETTINGS, AuthHttpInterceptor, provideIndiceAuth } from '@indice/ng-auth';
import { provideIcons, provideNgIconsConfig } from '@ng-icons/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { progressInterceptor } from 'ngx-progressbar/http';
import { provideQuillConfig } from 'ngx-quill/config';
import { routes } from './app.routes';
import { settings } from './core/models/settings';
import { AcceptLanguageHttpInterceptor } from './core/services/accept-language-http-interceptor.service';
import { CASES_API_BASE_URL } from './core/services/cases-api.service';
import { APP_ICONS } from './ui/icons';

export const appConfig: ApplicationConfig = {
  providers: [
    // zone.js stays: the JSON Forms renderers update their state imperatively.
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withXhr(), withInterceptors([progressInterceptor]), withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: AcceptLanguageHttpInterceptor, multi: true },
    provideIndiceAuth(),
    { provide: AUTH_SETTINGS, useFactory: () => settings.auth_settings },
    { provide: CASES_API_BASE_URL, useFactory: () => settings.api_url },
    provideTranslateService({
      loader: provideTranslateHttpLoader({ prefix: `${settings.api_url}/cases-i18n.`, useHttpBackend: true }),
      fallbackLang: 'en'
    }),
    provideIcons(APP_ICONS),
    provideNgIconsConfig({ size: '1.125rem', strokeWidth: 1.75 }),
    provideQuillConfig({
      modules: {
        toolbar: [
          ['bold', 'italic', 'underline'],
          [{ 'align': [] }],
          [{ 'list': 'ordered' }, { 'list': 'bullet' }],
          [{ 'indent': '-1' }, { 'indent': '+1' }],
          ['clean']
        ]
      }
    })
  ]
};
