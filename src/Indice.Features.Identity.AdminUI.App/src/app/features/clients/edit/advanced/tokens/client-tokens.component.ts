import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-client-tokens',
    templateUrl: './client-tokens.component.html'
})
export class ClientTokensComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(
        private _route: ActivatedRoute,
        private _clientStore: ClientStore,
        public utilities: UtilitiesService,
        public _toast: ToastService,
        private _authService: AuthService
    ) { }

    public client: SingleClientInfo;
    public canEditClient: boolean;

    public ngOnInit(): void {
        this.canEditClient = this._authService.isAdminUIClientsWriter();
        const clientId = this._route.parent.parent.snapshot.params['id'];
        this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: SingleClientInfo) => this.client = client);
    }

    public update(): void {
        this._clientStore.updateClient(this.client).subscribe(_ => {
            this._toast.showSuccess(`Client '${this.client.clientName}' was updated successfully.`);
        });
    }

    public hasAnyOf(types: string[]): boolean {
        return this.client.grantTypes && this.client.grantTypes.some(x => types.indexOf(x) > -1);
    }

    public hasAllOf(types: string[]): boolean {
        let result = true;
        if (this.client.grantTypes) {
            types.forEach((value: string) => {
                if (!this.client.grantTypes.includes(value)) {
                    result = false;
                }
            });
        }
        return result;
    }

    public containsCustomGrantTypes(): boolean {
        return this.client.grantTypes && this.client.grantTypes.some(x => !this.isKnownGrantType(x));
    }

    public isKnownGrantType(grantType: string): boolean {
        const knownGrantTypes = ['authorization_code', 'client_credentials', 'delegation', 'hybrid', 'implicit', 'password', 'refresh_token', 'urn:ietf:params:oauth:grant-type:device_code'];
        return knownGrantTypes.indexOf(grantType) > -1;
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }
}
