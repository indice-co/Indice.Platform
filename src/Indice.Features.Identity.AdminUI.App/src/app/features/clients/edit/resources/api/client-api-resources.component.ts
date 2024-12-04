import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { forkJoin, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from 'src/app/core/services/auth.service';
import { ApiResourceInfo, SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../../client-store.service';

@Component({
    selector: 'app-client-api-resources',
    templateUrl: './client-api-resources.component.html'
})
export class ClientApiResourcesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _addClientApiResourceSubscription: Subscription;
    private _deleteClientApiResourceSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute,
        private _clientStore: ClientStore,
        private _authService: AuthService
    ) { }

    public clientId = '';
    public availableResources: ApiResourceInfo[];
    public clientResources: ApiResourceInfo[];
    public canEditClient: boolean;
    public rows: ApiResourceInfo[] = [];
    public columns: TableColumn[] = [];
    public count = 0;

    public ngOnInit(): void {
        this.canEditClient = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: true },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: true, resizeable: true }
        ];
        this.clientId = this._route.parent.parent.snapshot.params['id'];
        const getClient = this._clientStore.getClient(this.clientId);
        const getApiScopes = this._clientStore.getApiScopes();
        this._getDataSubscription = forkJoin([getClient, getApiScopes]).pipe(map((responses: [SingleClientInfo, ApiResourceInfo[]]) => {
            return {
                client: responses[0],
                apiScopes: responses[1]
            };
        })).subscribe((result: { client: SingleClientInfo, apiScopes: ApiResourceInfo[] }) => {
            const clientApiResources = result.client.apiResources;
            const allApiResources = result.apiScopes;
            this.availableResources = allApiResources.filter(x => !clientApiResources.includes(x.name));
            this.clientResources = allApiResources.filter(x => clientApiResources.includes(x.name));
            this.count = this.clientResources.length;
            this.rows = this.clientResources;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._addClientApiResourceSubscription) {
            this._addClientApiResourceSubscription.unsubscribe();
        }
        if (this._deleteClientApiResourceSubscription) {
            this._deleteClientApiResourceSubscription.unsubscribe();
        }
    }

    public addResource(resource: ApiResourceInfo): void {
        this._addClientApiResourceSubscription = this._clientStore.addApiResource(this.clientId, resource).subscribe();
    }

    public removeResource(resource: ApiResourceInfo): void {
        this._deleteClientApiResourceSubscription = this._clientStore.deleteApiResource(this.clientId, resource).subscribe();
    }
}
