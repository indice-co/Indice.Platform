import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';

@Component({
    selector: 'app-client-tokens',
    templateUrl: './client-tokens.component.html'
})
export class ClientTokensComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, public utilities: UtilitiesService) { }

    public client: SingleClientInfo;

    public ngOnInit(): void {
        const clientId = this._route.parent.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public update(): void { }
}
