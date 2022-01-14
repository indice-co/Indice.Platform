import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { forkJoin, Subscription } from 'rxjs';
import { ExternalProvider, IdentityApiService, SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';
import { SelectableExternalProvider } from './selectable-external-provider.model';

@Component({
    selector: 'app-client-details',
    templateUrl: './client-details.component.html'
})
export class ClientDetailsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _deleteClientSubscription: Subscription;
    private _updateClientSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute,
        private _clientStore: ClientStore,
        public _toast: ToastService,
        private _router: Router,
        private _authService: AuthService,
        private _identityApi: IdentityApiService
    ) { }

    public client: SingleClientInfo;
    public externalProviders: SelectableExternalProvider[] = [];
    public canEditClient: boolean;

    public ngOnInit(): void {
        this.canEditClient = this._authService.isAdminUIClientsWriter();
        const clientId = this._route.parent.snapshot.params.id;
        const getClient$ = this._clientStore.getClient(clientId);
        const getExternalProviders$ = this._identityApi.getExternalProviders();
        this._getDataSubscription = forkJoin([getClient$, getExternalProviders$]).subscribe((result: [SingleClientInfo, ExternalProvider[]]) => {
            this.client = result[0];
            this.externalProviders = result[1].map((provider: ExternalProvider) =>
                new SelectableExternalProvider(this.client.identityProviderRestrictions.indexOf(provider.authenticationScheme) > -1 ? false : true, provider.displayName, provider.authenticationScheme));
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
        this._updateClientSubscription = this._clientStore.updateClient(this.client, this.externalProviders).subscribe(_ => {
            this._toast.showSuccess(`Client '${this.client.clientName}' was updated successfully.`);
        });
    }
}
