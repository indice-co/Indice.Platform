import { Injectable } from '@angular/core';
import { IdentityApiService, UiFeaturesInfo } from './identity-api.service';
import { AsyncSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UiFeaturesService {

  private _uiFeatureInfo: AsyncSubject<UiFeaturesInfo>;

  constructor(private _api: IdentityApiService) { }

  public getUiFeatures(): Observable<UiFeaturesInfo> {
    if (this._uiFeatureInfo) {
      return this._uiFeatureInfo;
    }
    this._uiFeatureInfo = new AsyncSubject<UiFeaturesInfo>();
    this._api.getUiFeatures()
      .subscribe((uiFeaturesInfo: UiFeaturesInfo) => {
        this._uiFeatureInfo.next(uiFeaturesInfo);
        this._uiFeatureInfo.complete();
      });
    return this._uiFeatureInfo;
  }
}