import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { forkJoin, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from 'src/app/core/services/auth.service';
import { SingleClientInfo, IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../../client-store.service';

@Component({
    selector: 'app-client-identity-resources',
    templateUrl: './client-identity-resources.component.html'
})
export class ClientIdentityResourcesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addClientIdentityResourceSubscription: Subscription;
    private _removeClientIdentityResourceSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute,
        private _clientStore: ClientStore,
        private _authService: AuthService
    ) { }

    public clientId = '';
    public availableResources: IdentityResourceInfo[];
    public clientResources: IdentityResourceInfo[];
    public canEditClient: boolean;
    public rows: IdentityResourceInfo[] = [];
    public columns: TableColumn[] = [];
    public count = 0;

    public ngOnInit(): void {
        this.canEditClient = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: true },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: true }
        ];
        this.clientId = this._route.parent.parent.snapshot.params.id;
        const getClient = this._clientStore.getClient(this.clientId);
        const getIdentityResources = this._clientStore.getIdentityResources();
        this._getDataSubscription = forkJoin([getClient, getIdentityResources]).pipe(map((responses: [SingleClientInfo, IdentityResourceInfo[]]) => {
            return {
                client: responses[0],
                identityResources: responses[1]
            };
        })).subscribe((result: { client: SingleClientInfo, identityResources: IdentityResourceInfo[] }) => {
            const clientIdentityResources = result.client.identityResources;
            const allIdentityResources = result.identityResources;
            this.availableResources = allIdentityResources.filter(x => !clientIdentityResources.includes(x.name));
            this.clientResources = allIdentityResources.filter(x => clientIdentityResources.includes(x.name));
            this.count = this.clientResources.length;
            this.rows = this.clientResources;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addClientIdentityResourceSubscription) {
            this._addClientIdentityResourceSubscription.unsubscribe();
        }
        if (this._removeClientIdentityResourceSubscription) {
            this._removeClientIdentityResourceSubscription.unsubscribe();
        }
    }

    public addResource(resource: IdentityResourceInfo): void {
        this._addClientIdentityResourceSubscription = this._clientStore.addIdentityResource(this.clientId, resource).subscribe();
    }

    public removeResource(resource: IdentityResourceInfo): void {
        this._removeClientIdentityResourceSubscription = this._clientStore.deleteIdentityResource(this.clientId, resource).subscribe();
    }
}
