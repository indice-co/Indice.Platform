import { HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable()
export class AcceptLanguageHttpInterceptor implements HttpInterceptor {

  public intercept(request: HttpRequest<any>, next: HttpHandler) {
    let modifiedRequest = request.clone({
      headers: request.headers.set('Accept-Language', 'el-GR,el;q=0.9,en;q=0.8'),
      params: request.params
    });
    return next.handle(modifiedRequest);
  }
}
