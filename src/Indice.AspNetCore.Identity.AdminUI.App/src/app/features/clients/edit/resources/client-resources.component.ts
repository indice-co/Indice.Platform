import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { ClientStore } from '../client-store.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';

@Component({
    selector: 'app-client-resources',
    templateUrl: './client-resources.component.html'
})
export class ClientResourcesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute,
        private _clientStore: ClientStore
    ) { }

    public client = new SingleClientInfo();

    public ngOnInit(): void {
        const clientId = this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public hideIdentityResources(): boolean {
        return (this.client?.grantTypes?.length === 1 && this.client.grantTypes[0] === 'client_credentials')
            || (this.client?.grantTypes?.length === 1 && this.client.grantTypes.indexOf('client_credentials') !== -1 && this.client.grantTypes.indexOf('delegation') !== -1);
    }
}
