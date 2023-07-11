import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanLoad, Route, RouterStateSnapshot, UrlSegment } from '@angular/router';
import { UiFeaturesService } from './ui-features.service';
import { Observable } from 'rxjs';
import { UiFeaturesInfo } from './identity-api.service';
import { map } from 'rxjs/operators';
import { Features } from '../models/features';

@Injectable({
  providedIn: 'root'
})
export class UiFeaturesGuardService implements CanActivate, CanLoad {
  constructor(private uiFeaturesService: UiFeaturesService) { }

  public canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
    return this.canProceed(route.data);
  }

  public canLoad(route: Route, segments: UrlSegment[]): Observable<boolean> | Promise<boolean> | boolean {
    return this.canProceed(route.data);
  }

  private canProceed(data): Observable<boolean> {
    return this.uiFeaturesService.getUiFeatures().pipe(map((uiFeaturesInfo: UiFeaturesInfo) => {
      if (data.feature == Features.Metrics) {
        return uiFeaturesInfo.metricsEnabled;
      }
      if (data.feature == Features.SignInLogs) {
        return uiFeaturesInfo.signInLogsEnabled;
      }
      return false;
    }));
  }
}