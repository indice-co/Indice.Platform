import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-client-urls',
    templateUrl: './client-urls.component.html'
})
export class ClientUrlsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore) { }

    public client: ClientInfo;

    public ngOnInit(): void {
        const clientId = this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: ClientInfo) => {
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
