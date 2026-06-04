import { HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable()
export class AcceptLanguageHttpInterceptor implements HttpInterceptor {

  constructor(private translate: TranslateService) { }

  public intercept(request: HttpRequest<any>, next: HttpHandler) {
    // Reflect the language the user selected (kept in sync by AppLanguagesService.setSelected ->
    // TranslateService.use), so server-side localized responses match the UI language.
    const lang = (this.translate.currentLang || this.translate.getFallbackLang() || 'el').toLowerCase();
    const acceptLanguage = `${lang},el;q=0.8`;
    const modifiedRequest = request.clone({
      headers: request.headers.set('Accept-Language', acceptLanguage),
      params: request.params
    });
    return next.handle(modifiedRequest);
  }
}
