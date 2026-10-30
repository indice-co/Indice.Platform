import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, shareReplay, tap } from 'rxjs';
import {
  ApiScopeInfo, ClientSecretInfo, ClientTranslation, CreateClaimRequest, CreateSecretRequest, FileParameter, IdentityApiService,
  IdentityResourceInfo, IUpdateClientRequest, SingleClientInfo, UpdateClientRequest, UpdateClientUrls
} from 'src/app/core/services/identity-api.service';

/**
 * State of the client open in the edit page, shared by its tabs. Provided by `ClientEditComponent`.
 * Every mutation goes to the API first and replaces the `client` signal with an updated copy on success.
 */
@Injectable()
export class ClientStore {
  private readonly _api = inject(IdentityApiService);
  private _identityResources$?: Observable<IdentityResourceInfo[]>;
  private _apiScopes$?: Observable<ApiScopeInfo[]>;

  public readonly client = signal<SingleClientInfo | null>(null);
  public readonly loading = signal(false);
  public readonly failed = signal(false);
  public readonly clientId = computed(() => this.client()?.clientId ?? '');
  public readonly grantTypes = computed(() => this.client()?.grantTypes ?? []);

  public load(clientId: string): void {
    this.loading.set(true);
    this.failed.set(false);
    this._api.getClient(clientId).subscribe({
      next: client => {
        this.client.set(client);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      }
    });
  }

  /** Saves the client with `changes` applied on top of its current state. */
  public update(changes: Partial<SingleClientInfo>): Observable<SingleClientInfo> {
    const client = new SingleClientInfo({ ...this.client()!, ...changes });
    const translations: { [key: string]: ClientTranslation } = {};
    for (const [culture, translation] of Object.entries(client.translations ?? {})) {
      translations[culture] = new ClientTranslation(translation);
    }
    const request: IUpdateClientRequest = {
      accessTokenLifetime: client.accessTokenLifetime,
      absoluteRefreshTokenLifetime: client.absoluteRefreshTokenLifetime,
      accessTokenType: client.accessTokenType,
      allowAccessTokensViaBrowser: client.allowAccessTokensViaBrowser,
      allowPlainTextPkce: client.allowPlainTextPkce,
      refreshTokenUsage: client.refreshTokenUsage,
      refreshTokenExpiration: client.refreshTokenExpiration,
      allowOfflineAccess: client.allowOfflineAccess,
      updateAccessTokenClaimsOnRefresh: client.updateAccessTokenClaimsOnRefresh,
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
      userSsoLifetime: client.userSsoLifetime,
      backChannelLogoutUri: client.backChannelLogoutUri,
      backChannelLogoutSessionRequired: client.backChannelLogoutSessionRequired,
      deviceCodeLifetime: client.deviceCodeLifetime,
      translations,
      userCodeType: client.userCodeType,
      enabled: client.enabled,
      slidingRefreshTokenLifetime: client.slidingRefreshTokenLifetime,
      enableLocalLogin: client.enableLocalLogin,
      identityProviderRestrictions: client.identityProviderRestrictions?.length ? client.identityProviderRestrictions : undefined
    };
    return this._api.updateClient(client.clientId!, new UpdateClientRequest(request)).pipe(
      map(() => client),
      tap(updated => this.client.set(updated))
    );
  }

  public updateUrls(urls: { redirectUris: string[]; allowedCorsOrigins: string[]; postLogoutRedirectUris: string[] }): Observable<void> {
    return this._api.updateClientUrls(this.clientId(), new UpdateClientUrls(urls)).pipe(tap(() => this.patch(urls)));
  }

  public getIdentityResources(): Observable<IdentityResourceInfo[]> {
    this._identityResources$ ??= this._api.getIdentityResources(1, 2147483647, 'name+', undefined).pipe(map(r => r.items ?? []), shareReplay(1));
    return this._identityResources$;
  }

  public getApiScopes(): Observable<ApiScopeInfo[]> {
    this._apiScopes$ ??= this._api.getApiScopes(1, 2147483647, 'name+', undefined).pipe(map(r => r.items ?? []), shareReplay(1));
    return this._apiScopes$;
  }

  public addResource(kind: 'apiResources' | 'identityResources', name: string): Observable<void> {
    return this._api.addClientResources(this.clientId(), [name]).pipe(
      tap(() => this.patch({ [kind]: [...(this.client()?.[kind] ?? []), name] }))
    );
  }

  public removeResource(kind: 'apiResources' | 'identityResources', name: string): Observable<void> {
    return this._api.deleteClientResource(this.clientId(), [name]).pipe(
      tap(() => this.patch({ [kind]: (this.client()?.[kind] ?? []).filter(x => x !== name) }))
    );
  }

  public addGrantType(grantType: string): Observable<void> {
    return this._api.addClientGrantType(this.clientId(), grantType).pipe(
      tap(created => this.patch({ grantTypes: [...this.grantTypes(), created.name ?? grantType] })),
      map(() => undefined)
    );
  }

  public removeGrantType(grantType: string): Observable<void> {
    return this._api.deleteClientGrantType(this.clientId(), grantType).pipe(
      tap(() => this.patch({ grantTypes: this.grantTypes().filter(x => x !== grantType) }))
    );
  }

  public addClaim(type: string, value: string): Observable<void> {
    return this._api.addClientClaim(this.clientId(), new CreateClaimRequest({ type, value })).pipe(
      tap(claim => this.patch({ claims: [...(this.client()?.claims ?? []), claim] })),
      map(() => undefined)
    );
  }

  public removeClaim(claimId: number): Observable<void> {
    return this._api.deleteClientClaim(this.clientId(), claimId).pipe(
      tap(() => this.patch({ claims: (this.client()?.claims ?? []).filter(x => x.id !== claimId) }))
    );
  }

  public addSharedSecret(secret: CreateSecretRequest): Observable<void> {
    return this._api.addClientSecret(this.clientId(), secret).pipe(tap(added => this.pushSecret(added)), map(() => undefined));
  }

  public addCertificate(file: File): Observable<void> {
    const parameter: FileParameter = { data: file, fileName: file.name };
    return this._api.uploadCertificate(this.clientId(), parameter).pipe(tap(added => this.pushSecret(added)), map(() => undefined));
  }

  public removeSecret(secretId: number): Observable<void> {
    return this._api.deleteClientSecret(this.clientId(), secretId).pipe(
      tap(() => this.patch({ secrets: (this.client()?.secrets ?? []).filter(x => x.id !== secretId) }))
    );
  }

  private pushSecret(secret: ClientSecretInfo): void {
    this.patch({ secrets: [...(this.client()?.secrets ?? []), secret] });
  }

  private patch(changes: Partial<SingleClientInfo>): void {
    const current = this.client();
    if (current) {
      this.client.set(new SingleClientInfo({ ...current, ...changes }));
    }
  }
}
