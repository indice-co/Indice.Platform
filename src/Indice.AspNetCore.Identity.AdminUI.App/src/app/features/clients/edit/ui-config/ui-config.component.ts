import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ClientUiConfigRequest, ClientUiConfigResponse, IdentityApiService } from 'src/app/core/services/identity-api.service';

@Component({
    selector: 'app-client-ui-config',
    templateUrl: './ui-config.component.html'
})
export class ClientUiConfigComponent implements OnInit {
    private _clientId: string;

    constructor(
        private _route: ActivatedRoute,
        private _identityApi: IdentityApiService
    ) { }

    public settings: any;

    public schemaFormOptions = {
        addSubmit: true,
        debug: false,
        loadExternalAssets: false,
        returnEmptyFields: false,
        setSchemaDefaults: true,
        defautWidgetOptions: { feedback: true }
    };

    public ngOnInit(): void {
        this._clientId = this._route.parent.snapshot.params.id;
        this._identityApi.getClientUiConfig(this._clientId).subscribe((response: ClientUiConfigResponse) => {
            this.settings = {
                schema: response.schema,
                layout: [
                    { "key": "backgroundImage", "title": "Background Image", "placeholder": "Image URL" },
                    { "key": "accentColor", "title": "Accent Color", "type": "color" },
                    { "key": "primaryColor", "title": "Primary Color", "type": "color" },
                    { "key": "secondaryColor", "title": "Secondary Color", "type": "color" }
                ],
                data: response.data
            };
            setTimeout(() => document.querySelectorAll('submit-widget > div > input')[0].className = 'btn btn-primary', 0);
        });
    }

    public onSubmitSettings(event: ClientUiConfigRequest): void {
        this._identityApi.createOrUpdateClientUiConfig(this._clientId, event).subscribe();
    }
}
