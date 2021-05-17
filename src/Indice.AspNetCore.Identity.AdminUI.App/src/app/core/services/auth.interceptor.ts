import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse } from '@angular/common/http';

import { Observable, of } from 'rxjs';
import { AuthService } from './auth.service';
import { catchError, tap } from 'rxjs/operators';
import { LoggerService } from './logger.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(
        private _authService: AuthService,
        private _logger: LoggerService
    ) { }

    public intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const copiedRequest = request.clone({
            headers: request.headers.set('Authorization', this._authService.getAuthorizationHeaderValue())
        });
        return next.handle(copiedRequest).pipe(
            tap((httpEvent: HttpEvent<any>) => {
                if (httpEvent instanceof HttpResponse) {
                    this._logger.log(httpEvent);
                }
            }),
            catchError((error: any) => {
                if (error.status === 401) {
                    this._authService.signoutRedirect();
                }
                this._logger.log(error);
                return of(error);
            })
        );
    }
}
