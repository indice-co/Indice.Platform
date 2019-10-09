import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { ClientInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ClientStore {
    private _client: AsyncSubject<ClientInfo>;

    constructor(private _api: IdentityApiService) { }

    public getClient(clientId: string): Observable<ClientInfo> {
        if (!this._client) {
            this._client = new AsyncSubject<ClientInfo>();
            this._api.getClient(clientId).subscribe((client: ClientInfo) => {
                this._client.next(client);
                this._client.complete();
            });
        }
        return this._client;
    }
}
