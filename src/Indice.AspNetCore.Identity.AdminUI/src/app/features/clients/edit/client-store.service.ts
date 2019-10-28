import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    IdentityApiService, SingleClientInfo, IdentityResourceInfoResultSet, IdentityResourceInfo, ApiResourceInfo, CreateClaimRequest, ClaimInfo, UpdateClientRequest, IUpdateClientRequest, 
    ScopeInfo, ScopeInfoResultSet
} from 'src/app/core/services/identity-api.service';
import { UrlType } from './urls/models/urlType';

@Injectable()
export class ClientStore {
    private _client: AsyncSubject<SingleClientInfo>;
    private _identityResources: AsyncSubject<IdentityResourceInfo[]>;
    private _apiScopes: AsyncSubject<ScopeInfo[]>;

    constructor(private _api: IdentityApiService) { }

    public getClient(clientId: string): Observable<SingleClientInfo> {
        if (!this._client) {
            this._client = new AsyncSubject<SingleClientInfo>();
            this._api.getClient(clientId).subscribe((client: SingleClientInfo) => {
                this._client.next(client);
                this._client.complete();
            });
        }
        return this._client;
    }

    public updateClient(client: SingleClientInfo): Observable<void> {
        return this._api.updateClient(client.clientId, new UpdateClientRequest({
            accessTokenLifetime: client.accessTokenLifetime,
            accessTokenType: client.accessTokenType,
            allowAccessTokensViaBrowser: client.allowAccessTokensViaBrowser,
            allowPlainTextPkce: client.allowPlainTextPkce,
            allowRememberConsent: client.allowRememberConsent,
            alwaysIncludeUserClaimsInIdToken: client.alwaysIncludeUserClaimsInIdToken,
            alwaysSendClientClaims: client.alwaysSendClientClaims,
            authorizationCodeLifetime: client.authorizationCodeLifetime,
            clientClaimsPrefix: client.clientClaimsPrefix,
            clientName: client.clientName,
            clientUri: client.clientUri,
            consentLifetime: client.consentLifetime,
            description: client.description,
            frontChannelLogoutSessionRequired: client.frontChannelLogoutSessionRequired,
            frontChannelLogoutUri: client.frontChannelLogoutUri,
            identityTokenLifetime: client.identityTokenLifetime,
            includeJwtId: client.includeJwtId,
            logoUri: client.logoUri,
            pairWiseSubjectSalt: client.pairWiseSubjectSalt,
            requireConsent: client.requireConsent,
            requirePkce: client.requirePkce,
            userSsoLifetime: client.userSsoLifetime
        } as IUpdateClientRequest)).pipe(map(_ => {
            this._client.next(client);
            this._client.complete();
        }));
    }

    public updateClientUrl(clientId: string, url: string, added: boolean, urlType: UrlType): void {
        this.getClient(clientId).subscribe((client: SingleClientInfo) => {
            switch (urlType) {
                case UrlType.Redirect:
                    if (added) {
                        client.redirectUris.push(url);
                    } else {
                        const index = client.redirectUris.findIndex(x => x === url);
                        if (index > -1) {
                            client.redirectUris.splice(index, 1);
                        }
                    }
                    break;
                case UrlType.Cors:
                    if (added) {
                        client.allowedCorsOrigins.push(url);
                    } else {
                        const index = client.allowedCorsOrigins.findIndex(x => x === url);
                        if (index > -1) {
                            client.allowedCorsOrigins.splice(index, 1);
                        }
                    }
                    break;
                case UrlType.PostLogoutRedirect:
                    if (added) {
                        client.postLogoutRedirectUris.push(url);
                    } else {
                        const index = client.postLogoutRedirectUris.findIndex(x => x === url);
                        if (index > -1) {
                            client.postLogoutRedirectUris.splice(index, 1);
                        }
                    }
                    break;
                default:
                    break;
            }
            this._client.next(client);
            this._client.complete();
        });
    }

    public getIdentityResources(): Observable<IdentityResourceInfo[]> {
        if (!this._identityResources) {
            this._identityResources = new AsyncSubject<IdentityResourceInfo[]>();
            this._api.getIdentityResources(1, 2147483647, 'name+', undefined).subscribe((response: IdentityResourceInfoResultSet) => {
                this._identityResources.next(response.items);
                this._identityResources.complete();
            });
        }
        return this._identityResources;
    }

    public getApiScopes(): Observable<ScopeInfo[]> {
        if (!this._apiScopes) {
            this._apiScopes = new AsyncSubject<ApiResourceInfo[]>();
            this._api.getApiScopes(1, 2147483647, 'name+', undefined).subscribe((response: ScopeInfoResultSet) => {
                this._apiScopes.next(response.items);
                this._apiScopes.complete();
            });
        }
        return this._apiScopes;
    }

    public addClaim(clientId: string, claim: ClaimInfo): Observable<void> {
        return this._api.addClientClaim(clientId, {
            type: claim.type,
            value: claim.value
        } as CreateClaimRequest).pipe(map((createdClaim: ClaimInfo) => {
            this.getClient(clientId).subscribe((client: SingleClientInfo) => {
                client.claims.push(createdClaim);
                this._client.next(client);
                this._client.complete();
            });
        }));
    }

    public addApiResource(clientId: string, resource: ApiResourceInfo): Observable<void> {
        this.getClient(clientId).subscribe((client: SingleClientInfo) => {
            client.apiResources.push(resource.name);
            this._client.next(client);
            this._client.complete();
        });
        return this._api.addClientResources(clientId, [resource.name]);
    }

    public deleteApiResource(clientId: string, resource: ApiResourceInfo): Observable<void> {
        this.getClient(clientId).subscribe((client: SingleClientInfo) => {
            const index = client.apiResources.findIndex(x => x === resource.name);
            if (index > -1) {
                client.apiResources.splice(index, 1);
            }
            this._client.next(client);
            this._client.complete();
        });
        return this._api.deleteClientResource(clientId, resource.name);
    }

    public addIdentityResource(clientId: string, resource: IdentityResourceInfo): Observable<void> {
        this.getClient(clientId).subscribe((client: SingleClientInfo) => {
            client.identityResources.push(resource.name);
            this._client.next(client);
            this._client.complete();
        });
        return this._api.addClientResources(clientId, [resource.name]);
    }

    public deleteClient(clientId: string): Observable<void> {
        return this._api.deleteClient(clientId);
    }
}
