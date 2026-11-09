import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { settings } from '../models/settings';
import { AppLanguagesService, DEFAULT_LANGUAGE } from './app-languages.service';

/**
 * Lets the identity API localize its responses (e.g. validation messages, password rules) in the UI language. Besides
 * `Accept-Language`, the `ui-culture` query parameter is sent too, because hosts may disable the header based culture
 * provider (the identity pages template does) while the query string provider is the default first one.
 */
export const acceptLanguageInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(`${settings.api_url}/api/`)) {
    return next(request);
  }
  const language = inject(AppLanguagesService).current();
  const value = language === DEFAULT_LANGUAGE ? language : `${language},${DEFAULT_LANGUAGE};q=0.8`;
  return next(request.clone({
    setHeaders: { 'Accept-Language': value },
    setParams: { 'ui-culture': language }
  }));
};
