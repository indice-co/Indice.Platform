import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { forkJoin, Subscription } from 'rxjs';
import { SingleClientInfo, ClientTranslation, ExternalProvider, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { TranslateInputService } from 'src/app/shared/components/translate-input/translate-input.service';
import { SelectableExternalProvider } from './selectable-external-provider.model';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-client-details',
    templateUrl: './client-details.component.html',
    providers: [TranslateInputService]
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
        private _identityApi: IdentityApiService,
        private _translateInputService: TranslateInputService
    ) { }

    public client: SingleClientInfo;
    public externalProviders: SelectableExternalProvider[] = [];
    public canEditClient: boolean;
    public selectedCulture: string;
    public cultures = ['EL', 'EN'];
    public nameTranslations: { [key: string]: string; } = {};
    public descriptionTranslations: { [key: string]: string; } = {};

    public ngOnInit(): void {
        this.canEditClient = this._authService.isAdminUIClientsWriter();
        const clientId = this._route.parent.snapshot.params.id;
        const getClient$ = this._clientStore.getClient(clientId);
        const getExternalProviders$ = this._identityApi.getExternalProviders();
        this._getDataSubscription = forkJoin([getClient$, getExternalProviders$]).subscribe((result: [SingleClientInfo, ExternalProvider[]]) => {
            this.client = result[0];
            this.externalProviders = result[1].map((provider: ExternalProvider) =>
                new SelectableExternalProvider(this.client.identityProviderRestrictions.indexOf(provider.authenticationScheme) > -1 ? false : true, provider.displayName, provider.authenticationScheme));
            if (!this.client.translations) {
                this.client.translations = {} as { [key: string]: ClientTranslation; };
                return;
            }
            this.nameTranslations = this._translateInputService.getPropertyTranslations('clientName', this.client);
            this.descriptionTranslations = this._translateInputService.getPropertyTranslations('description', this.client);
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

    public onCultureChanged(culture: string) {
        this.selectedCulture = culture;
    }

    public getClientNameTranslations(translations: { [key: string]: string; }): void {
        this.setPropertyTranslation('clientName', translations, this.client);
    }

    public getDescriptionTranslations(translations: { [key: string]: string; }): void {
        this.setPropertyTranslation('description', translations, this.client);
    }

    public setPropertyTranslation(propertyPath: string | Array<string>, translations: { [key: string]: string; }, obj: any): void {
        this._translateInputService.setPrimaryLanguageTranslation(propertyPath, translations, obj);
        this._translateInputService.setOtherLanguageTranslation(propertyPath, translations, obj, true);
    }
}
