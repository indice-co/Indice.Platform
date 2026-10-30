import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable, map, shareReplay } from 'rxjs';
import { Features } from '../models/features';
import { IdentityApiService, UiFeaturesInfo } from './identity-api.service';

/** Server side feature switches (sign in logs, activity logs, metrics), fetched once and cached. */
@Injectable({
  providedIn: 'root'
})
export class UiFeaturesService {
  private readonly _features$ = inject(IdentityApiService).getUiFeatures().pipe(shareReplay({ bufferSize: 1, refCount: false }));

  /** The feature switches; `undefined` until loaded. */
  public readonly features = toSignal(this._features$);

  public getUiFeatures(): Observable<UiFeaturesInfo> {
    return this._features$;
  }

  public isEnabled(feature: Features): Observable<boolean> {
    return this._features$.pipe(map(info => isFeatureEnabled(info, feature)));
  }
}

export function isFeatureEnabled(info: UiFeaturesInfo | undefined, feature: Features): boolean {
  switch (feature) {
    case Features.Metrics: return !!info?.metricsEnabled;
    case Features.SignInLogs: return !!info?.signInLogsEnabled;
    case Features.ActivityLogs: return !!info?.activityLogsEnabled;
    default: return false;
  }
}
