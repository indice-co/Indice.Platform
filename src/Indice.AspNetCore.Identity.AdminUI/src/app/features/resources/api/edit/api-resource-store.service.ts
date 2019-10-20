import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { ApiResourceInfo, IdentityApiService, ScopeInfo } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ApiResourceStore {
    private _apiResource: AsyncSubject<ApiResourceInfo>;

    constructor(private _api: IdentityApiService) { }

    public getApiResource(apiResourceId: number): Observable<ApiResourceInfo> {
        if (!this._apiResource) {
            this._apiResource = new AsyncSubject<ApiResourceInfo>();
            this._api.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
                apiResource.scopes = apiResource.scopes.sort((x: ScopeInfo, y: ScopeInfo) => (x.name > y.name ? 1 : -1));
                this._apiResource.next(apiResource);
                this._apiResource.complete();
            });
        }
        return this._apiResource;
    }
}
