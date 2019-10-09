import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-client-details',
    templateUrl: './client-details.component.html'
})
export class ClientDetailsComponent implements OnInit, OnDestroy {
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore) { }

    public client: SingleClientInfo;

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

    public deletePrompt(): void {
        this._deleteAlert.fire();
    }

    public delete(): void { }

    public update(): void { }
}
