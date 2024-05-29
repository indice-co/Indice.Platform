import { Inject, Injectable } from '@angular/core';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';

import { ToasterService, ToastType } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { UtilitiesService } from '../shared/utilities.service';

@Injectable()
export class BadRequestInterceptor implements HttpInterceptor {
    constructor(
        @Inject(ToasterService) private _toaster: ToasterService,
        private _utilities: UtilitiesService
    ) { }

    public intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return next.handle(request).pipe(
            catchError((error: HttpResponse<any>) => {
                if (error instanceof HttpErrorResponse && error.status === 400) {
                    const fileReader = new FileReader();
                    fileReader.addEventListener('loadend', () => {
                        const problemDetails = fileReader.result!;
                        this._toaster.show(ToastType.Error, 'general.failed-request', `${this._utilities.getValidationErrors(JSON.parse(problemDetails.toString()))}`, 6000);
                    });
                    fileReader.readAsText(error.error);
                }
                throw error;
            })
        );
    }
}
