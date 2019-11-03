import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';

import { Subscription, from, forkJoin } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo, ClaimInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { environment } from 'src/environments/environment';
import { map } from 'rxjs/operators';

@Component({
    selector: 'app-client-grant-types',
    templateUrl: './client-grant-types.component.html'
})
export class ClientGrantTypesComponent implements OnInit, OnDestroy {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('grantTypesform', { static: false }) private _form: NgForm;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, private _toast: ToastService, private _httpClient: HttpClient) { }

    public client: SingleClientInfo;
    public columns: TableColumn[] = [];
    public selectedGrantType = '';
    public rows: { type: string }[];
    public availableGrantTypes: string[];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'type', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        const clientId = this._route.parent.parent.snapshot.params.id;
        const getClient = this._clientStore.getClient(clientId);
        const getDiscoveryDocument = this._httpClient.get(`${environment.auth_settings.authority}/.well-known/openid-configuration`);
        this._getDataSubscription = forkJoin([getClient, getDiscoveryDocument]).pipe(map((responses: [SingleClientInfo, any]) => {
            return {
                client: responses[0],
                discoveryResponse: responses[1]
            };
        })).subscribe((result: { client: SingleClientInfo, discoveryResponse: any }) => {
            this.client = result.client;
            this.rows = this.client.grantTypes.map((value: string) => {
                return {
                    type: value
                };
            });
            let grantTypes = result.discoveryResponse.grant_types_supported as string[];
            grantTypes.push('hybrid');
            grantTypes = grantTypes.sort((left: string, right: string) => (left > right ? 1 : -1));
            this.availableGrantTypes = grantTypes;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public addGrantType(): void {
        this._clientStore.addClaim(this.client.clientId, {
            type: this.selectedGrantType,
            // value: this.selectedClaimValue
        } as ClaimInfo).subscribe(_ => {
            this._toast.showSuccess(`Claim '${this.selectedGrantType}' was successfully added to the client.`);
            this._form.resetForm({
                'claim-name': '',
                'claim-value': ''
            });
            this.rows = [...this.rows];
        });
    }

    public update(): void { }
}
