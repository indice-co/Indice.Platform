import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { forkJoin, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
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

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore) { }

    public clientId = '';
    public availableResources: ApiResourceInfo[];
    public clientResources: ApiResourceInfo[];

    public ngOnInit(): void {
        this.clientId = this._route.parent.parent.snapshot.params.id;
        const getClient = this._clientStore.getClient(this.clientId);
        const getApiResources = this._clientStore.getApiResources();
        this._getDataSubscription = forkJoin([getClient, getApiResources]).pipe(map((responses: [SingleClientInfo, ApiResourceInfo[]]) => {
            return {
                client: responses[0],
                apiResources: responses[1]
            };
        })).subscribe((result: { client: SingleClientInfo, apiResources: ApiResourceInfo[] }) => {
            const clientApiResources = result.client.apiResources;
            const allApiResources = result.apiResources;
            this.availableResources = allApiResources.filter(x => !clientApiResources.includes(x.name));
            this.clientResources = allApiResources.filter(x => clientApiResources.includes(x.name));
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
