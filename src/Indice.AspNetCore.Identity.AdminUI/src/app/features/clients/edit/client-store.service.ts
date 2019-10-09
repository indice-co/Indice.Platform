import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { IdentityApiService, SingleClientInfo } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ClientStore {
    private _client: AsyncSubject<SingleClientInfo>;

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
}
