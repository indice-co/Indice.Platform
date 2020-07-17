import { Injectable } from '@angular/core';

import { AsyncSubject, Observable, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    ApiResourceInfo, IdentityApiService, ScopeInfo, ClaimTypeInfo, ClaimTypeInfoResultSet, UpdateApiResourceRequest, IUpdateApiResourceRequest, CreateApiScopeRequest,
    UpdateApiScopeRequest, IUpdateApiScopeRequest, CreateSecretRequest, ApiSecretInfo, ICreateSecretRequest, SecretInfo
} from 'src/app/core/services/identity-api.service';

@Injectable()
export class ApiResourceStore {
    private _apiResource: AsyncSubject<ApiResourceInfo>;
    private _allClaims: AsyncSubject<ClaimTypeInfo[]>;

    constructor(private _api: IdentityApiService) { }

    public getApiResource(apiResourceId: number): Observable<ApiResourceInfo> {
        if (!this._apiResource) {
            this._apiResource = new AsyncSubject<ApiResourceInfo>();
            this._api.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
                apiResource.secrets = apiResource.secrets || [];
                apiResource.scopes = apiResource.scopes.sort((left: ScopeInfo, right: ScopeInfo) => (left.scope > right.scope ? 1 : -1));
                apiResource.scopes.forEach((value: ScopeInfo) => {
                    (value as any).isOpen = false;
                    value.userClaims = value.userClaims || [];
                });
                this._apiResource.next(apiResource);
                this._apiResource.complete();
            });
        }
        return this._apiResource;
    }

    public updateApiResource(apiResource: ApiResourceInfo): Observable<void> {
        return this._api.updateApiResource(apiResource.id, new UpdateApiResourceRequest({
            displayName: apiResource.displayName,
            description: apiResource.description,
            enabled: apiResource.enabled
        } as IUpdateApiResourceRequest)).pipe(map(_ => {
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        }));
    }

    public addApiResourceClaim(apiResourceId: number, claim: ClaimTypeInfo): Observable<void> {
        const getApiResource = this.getApiResource(apiResourceId);
        const addClaim = this._api.addApiResourceClaims(apiResourceId, [claim.name]);
        return forkJoin([getApiResource, addClaim]).pipe(map((responses: [ApiResourceInfo, void]) => {
            return {
                apiResource: responses[0]
            };
        })).pipe(map((result: { apiResource: ApiResourceInfo }) => {
            result.apiResource.allowedClaims.push(claim.name);
            this._apiResource.next(result.apiResource);
            this._apiResource.complete();
        }));
    }

    public addApiResourceSecret(apiResourceId: number, secret: CreateSecretRequest): Observable<void> {
        const getApiResource = this.getApiResource(apiResourceId);
        const addSecret = this._api.addApiResourceSecret(apiResourceId, secret);
        return forkJoin([getApiResource, addSecret]).pipe(map((responses: [ApiResourceInfo, SecretInfo]) => {
            return {
                apiResource: responses[0],
                addedSecret: responses[1]
            };
        })).pipe(map((result: { apiResource: ApiResourceInfo, addedSecret: ApiSecretInfo }) => {
            (result.addedSecret as any).valueText = 'Value is hidden';
            result.apiResource.secrets.push(result.addedSecret);
            this._apiResource.next(result.apiResource);
            this._apiResource.complete();
        }));
    }

    public deleteApiResourceSecret(apiResourceId: number, secret: ApiSecretInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const index = apiResource.secrets.indexOf(secret);
            if (index > -1) {
                apiResource.secrets.splice(index, 1);
            }
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.deleteApiResourceSecret(apiResourceId, secret.id);
    }

    public deleteApiResourceClaim(apiResourceId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const index = apiResource.allowedClaims.indexOf(claim.name);
            if (index > -1) {
                apiResource.allowedClaims.splice(index, 1);
            }
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.deleteApiResourceClaim(apiResourceId, claim.name);
    }

    public addApiResourceScope(apiResourceId: number, scope: CreateApiScopeRequest): Observable<void> {
        const getApiResource = this.getApiResource(apiResourceId);
        scope.showInDiscoveryDocument = true;
        const addScope = this._api.addApiResourceScope(apiResourceId, scope);
        return forkJoin([getApiResource, addScope]).pipe(map((responses: [ApiResourceInfo, ScopeInfo]) => {
            return {
                apiResource: responses[0],
                addedScope: responses[1]
            };
        })).pipe(map((result: { apiResource: ApiResourceInfo, addedScope: ScopeInfo }) => {
            result.apiResource.scopes.push(result.addedScope);
            result.apiResource.scopes = result.apiResource.scopes.sort((left: ScopeInfo, right: ScopeInfo) => (left.scope > right.scope ? 1 : -1));
            this._apiResource.next(result.apiResource);
            this._apiResource.complete();
        }));
    }

    public updateApiResourceScope(apiResourceId: number, scope: ScopeInfo) {
        const getApiResource = this.getApiResource(apiResourceId);
        const updateScope = this._api.updateApiResourceScope(apiResourceId, scope.id, new UpdateApiScopeRequest({
            displayName: scope.displayName,
            description: scope.description,
            emphasize: scope.emphasize,
            nonEditable: scope.nonEditable,
            showInDiscoveryDocument: scope.showInDiscoveryDocument
        } as IUpdateApiScopeRequest));
        return forkJoin([getApiResource, updateScope]).pipe(map((responses: [ApiResourceInfo, null]) => {
            return {
                apiResource: responses[0],
                updatedScope: responses[1]
            };
        })).pipe(map((result: { apiResource: ApiResourceInfo }) => {
            let foundScope = result.apiResource.scopes.find(x => x.id === scope.id);
            if (foundScope) {
                foundScope = scope;
            }
            this._apiResource.next(result.apiResource);
            this._apiResource.complete();
        }));
    }

    public deleteApiResourceScope(apiResourceId: number, scopeId: number): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const index = apiResource.scopes.findIndex(x => x.id === scopeId);
            if (index > -1) {
                apiResource.scopes.splice(index, 1);
            }
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.deleteApiResourceScope(apiResourceId, scopeId);
    }

    public addApiResourceScopeClaim(apiResourceId: number, scopeId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const scopeClaims = apiResource.scopes.find(x => x.id === scopeId).userClaims;
            scopeClaims.push(claim.name);
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.addApiResourceScopeClaims(apiResourceId, scopeId, [claim.name]);
    }

    public deleteApiResourceScopeClaim(apiResourceId: number, scopeId: number, claim: ClaimTypeInfo): Observable<void> {
        this.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            const scope = apiResource.scopes.find(x => x.id === scopeId);
            const index = scope.userClaims.indexOf(claim.name);
            if (index > -1) {
                scope.userClaims.splice(index, 1);
            }
            this._apiResource.next(apiResource);
            this._apiResource.complete();
        });
        return this._api.deleteApiResourceScopeClaim(apiResourceId, scopeId, claim.name);
    }

    public deleteApiResource(resourceId: number): Observable<void> {
        return this._api.deleteApiResource(resourceId);
    }

    public getAllClaims(): Observable<ClaimTypeInfo[]> {
        if (!this._allClaims) {
            this._allClaims = new AsyncSubject<ClaimTypeInfo[]>();
            this._api.getClaimTypes(undefined, 1, 2147483647, 'name+', undefined).subscribe((response: ClaimTypeInfoResultSet) => {
                this._allClaims.next(response.items);
                this._allClaims.complete();
            });
        }
        return this._allClaims;
    }
}
