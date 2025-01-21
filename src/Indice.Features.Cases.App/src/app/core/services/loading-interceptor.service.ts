import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { ProgressBarService } from './progress-bar.service';

@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  private activeRequests: number = 0;

  constructor(private progressBarService: ProgressBarService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (this.activeRequests === 0) {
      this.progressBarService.start(); // Start the progress bar when the first request begins
    }

    this.activeRequests++;
    this.progressBarService.setProgress(10); // Set initial progress

    return next.handle(req).pipe(
      tap(() => {
        this.progressBarService.setProgress(50); // Increment progress during the request
      }),
      catchError((error) => {
        this.progressBarService.setProgress(90); // Advance to near completion on error
        throw error;
      }),
      finalize(() => {
        this.activeRequests--;
        if (this.activeRequests === 0) {
          this.progressBarService.complete(); // Complete progress bar when all requests finish
        }
      })
    );
  }
}
