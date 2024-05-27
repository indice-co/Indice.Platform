import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ClientThemeConfigRequest, ClientThemeConfigResponse, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-client-theme-config',
    templateUrl: './theme.component.html'
})
export class ClientUiConfigComponent implements OnInit {
    private _clientId: string;

    constructor(
        private _route: ActivatedRoute,
        private _identityApi: IdentityApiService,
        private _toast: ToastService
    ) { }

    public settings: any;

    public schemaFormOptions = {
        addSubmit: true,
        debug: false,
        loadExternalAssets: false,
        returnEmptyFields: false,
        setSchemaDefaults: false,
        defautWidgetOptions: { feedback: true }
    };

    public ngOnInit(): void {
        this._clientId = this._route.parent.snapshot.params['id'];
        this._identityApi.getClientTheme(this._clientId).subscribe((response: ClientThemeConfigResponse) => {
            this.settings = {
                schema: response.schema,
                data: response.data
            };
            setTimeout(() => document.querySelectorAll('submit-widget > div > input')[0].className = 'btn btn-primary', 0);
        });
    }

    public onSubmitSettings(event: ClientThemeConfigRequest): void {
        this._identityApi.createOrUpdateClientTheme(this._clientId, event).subscribe(() => {
            this._toast.showSuccess(`Client theme configuration was saved successfully.`);
        });
    }
}
