import { HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable()
export class AcceptLanguageHttpInterceptor implements HttpInterceptor {

  constructor(private translate: TranslateService) { }

    public intercept(request: HttpRequest<any>, next: HttpHandler) {
    const lang = (this.translate.getCurrentLang() || this.translate.getFallbackLang() || 'en').toLowerCase();
    const acceptLanguage = `${lang},en;q=0.8`;
    const modifiedRequest = request.clone({
      headers: request.headers.set('Accept-Language', acceptLanguage),
      params: request.params
    });
    return next.handle(modifiedRequest);
  }
}
