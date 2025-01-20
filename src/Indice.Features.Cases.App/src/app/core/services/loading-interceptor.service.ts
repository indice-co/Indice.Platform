import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { LoadingBarService } from './loading-bar.service';

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  private activeRequests: number = 0;

  constructor(private loadingBarService: LoadingBarService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (this.activeRequests === 0) {
      this.loadingBarService.start(); // Start the progress bar when the first request begins
    }

    this.activeRequests++;
    this.loadingBarService.setProgress(10); // Set initial progress

    return next.handle(req).pipe(
      tap(() => {
        this.loadingBarService.setProgress(50); // Increment progress during the request
      }),
      catchError((error) => {
        this.loadingBarService.setProgress(90); // Advance to near completion on error
        throw error;
      }),
      finalize(() => {
        this.activeRequests--;
        if (this.activeRequests === 0) {
          this.loadingBarService.complete(); // Complete progress bar when all requests finish
        }
      })
    );
  }
}
