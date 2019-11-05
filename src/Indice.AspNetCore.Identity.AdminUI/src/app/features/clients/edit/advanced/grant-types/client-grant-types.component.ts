import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { TableColumn } from '@swimlane/ngx-datatable';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { environment } from 'src/environments/environment';
import { GrantTypeStateMatrixService } from './grant-type-state-matrix.service';

@Component({
    selector: 'app-client-grant-types',
    templateUrl: './client-grant-types.component.html',
    providers: [GrantTypeStateMatrixService]
})
export class ClientGrantTypesComponent implements OnInit, OnDestroy {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('grantTypesform', { static: false }) private _form: NgForm;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, private _toast: ToastService, private _httpClient: HttpClient, private _grantTypeStateMatrixService: GrantTypeStateMatrixService) { }

    public client: SingleClientInfo;
    public columns: TableColumn[] = [];
    public selectedGrantType = '';
    public rows: { type: string }[];
    public availableGrantTypes: string[];
    public grantTypeToDelete: { type: string };

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
        this._clientStore.addGrantType(this.client.clientId, this.selectedGrantType).subscribe(_ => {
            this._toast.showSuccess(`Grant type '${this.selectedGrantType}' was successfully added to the client.`);
            this.rows.push({ type: this.selectedGrantType });
            this._form.resetForm({
                'grant-type': ''
            });
            this.rows = [...this.client.grantTypes.map((value: string) => {
                return {
                    type: value
                };
            })];
        });
    }

    public delete(): void {
        this._clientStore.deleteGrantType(this.client.clientId, this.grantTypeToDelete.type).subscribe(_ => {
            this._toast.showSuccess(`Grant type '${this.selectedGrantType}' was successfully removed from the client.`);
            const index = this.rows.findIndex(x => x.type === this.grantTypeToDelete.type);
            if (index > - 1) {
                this.rows.splice(index, 1);
            }
            this.rows = [...this.client.grantTypes.map((value: string) => {
                return {
                    type: value
                };
            })];
        });
    }

    public canAddGrantType(grantType: string): boolean {
        // A grant type that is already owned by the client, cannot be added again.
        if (this.client.grantTypes.indexOf(grantType) > -1) {
            return false;
        }
        let result = true;
        // Check if grant type in the list can be added to client according to the grant types that already owns.
        for (const type of this.client.grantTypes) {
            if (!this._grantTypeStateMatrixService.canGoTo(type, grantType)) {
                result = false;
                break;
            }
        }
        return result;
    }

    public showDeleteAlert(grantType: string): void {
        this.grantTypeToDelete = this.rows.find(x => x.type === grantType);
        setTimeout(() => this._deleteAlert.fire(), 0);
    }
}
