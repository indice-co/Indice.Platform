import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-client-details',
    templateUrl: './client-details.component.html'
})
export class ClientDetailsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _deleteClientSubscription: Subscription;
    private _updateClientSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, public _toast: ToastService, private _router: Router) { }

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
        if (this._deleteClientSubscription) {
            this._deleteClientSubscription.unsubscribe();
        }
        if (this._updateClientSubscription) {
            this._updateClientSubscription.unsubscribe();
        }
    }

    public delete(): void {
        this._deleteClientSubscription = this._clientStore.deleteClient(this.client.clientId).subscribe(_ => {
            this._toast.showSuccess(`Client '${this.client.clientName}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        this._updateClientSubscription = this._clientStore.updateClient(this.client).subscribe(_ => {
            this._toast.showSuccess(`Client '${this.client.clientName}' was updated successfully.`);
        });
    }
}
