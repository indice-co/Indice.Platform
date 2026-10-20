import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { AUTH_SETTINGS, AuthHttpInterceptor, provideIndiceAuth } from '@indice/ng-auth';
import { provideLucideConfig } from '@lucide/angular';
import { progressInterceptor } from 'ngx-progressbar/http';
import { provideNgxMask } from 'ngx-mask';
import { provideQuillConfig } from 'ngx-quill';
import { settings } from './core/models/settings';
import { CASES_API_BASE_URL } from './core/services/cases-api.service';
import { AcceptLanguageHttpInterceptor } from './core/services/accept-language-http-interceptor.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
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
    provideLucideConfig({ strokeWidth: 1.75 }),
    provideNgxMask(),
    provideQuillConfig({
      modules: {
        toolbar: [
          ['bold', 'italic', 'underline'],
          [{ align: [] }],
          [{ list: 'ordered' }, { list: 'bullet' }],
          [{ indent: '-1' }, { indent: '+1' }],
          ['clean']
        ]
      }
    })
  ]
};
