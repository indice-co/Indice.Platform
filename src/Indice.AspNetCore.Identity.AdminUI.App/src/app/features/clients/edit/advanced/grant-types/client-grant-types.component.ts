import { ActivatedRoute } from '@angular/router';
import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NgForm } from '@angular/forms';

import { map } from 'rxjs/operators';
import { Subscription, forkJoin } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { TableColumn } from '@swimlane/ngx-datatable';
import { ClientStore } from '../../client-store.service';
import { GrantTypeStateMatrixService } from './grant-type-state-matrix.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import * as app from 'src/app/core/models/settings';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-client-grant-types',
    templateUrl: './client-grant-types.component.html',
    providers: [GrantTypeStateMatrixService]
})
export class ClientGrantTypesComponent implements OnInit, OnDestroy {
    constructor(
        private clientStore: ClientStore,
        private grantTypeStateMatrixService: GrantTypeStateMatrixService,
        private httpClient: HttpClient,
        private route: ActivatedRoute,
        private toast: ToastService,
        private _authService: AuthService
    ) { }

    @ViewChild('actionsTemplate', { static: true })
    private actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('grantTypesform', { static: false })
    private form: NgForm;
    @ViewChild('deleteAlert', { static: false })
    private deleteAlert: SwalComponent;
    private getDataSubscription: Subscription;
    public client: SingleClientInfo;
    public columns: TableColumn[] = [];
    public selectedGrantType = '';
    public rows: { type: string }[];
    public availableGrantTypes: string[];
    public grantTypeToDelete: { type: string };
    public customGrantName: string;
    public canEditClient: boolean;

    public ngOnInit(): void {
        this.canEditClient = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false }
        ];
        if (this.canEditClient) {
            this.columns.push({ prop: 'type', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this.actionsTemplate, cellClass: 'd-flex align-items-center' });
        }
        const clientId = this.route.parent.parent.snapshot.params.id;
        const getClient = this.clientStore.getClient(clientId);
        const getDiscoveryDocument = this.httpClient.get(`${app.settings.auth_settings.authority}/.well-known/openid-configuration`);
        this.getDataSubscription = forkJoin([getClient, getDiscoveryDocument]).pipe(map((responses: [SingleClientInfo, any]) => {
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

    public addGrantType(): void {
        const grantToAdd = this.selectedGrantType === 'custom' ? this.customGrantName : this.selectedGrantType;
        this.clientStore.addGrantType(this.client.clientId, grantToAdd).subscribe(_ => {
            this.toast.showSuccess(`Grant type '${grantToAdd}' was successfully added to client '${this.client.clientName}'.`);
            this.rows.push({ type: grantToAdd });
            this.form.resetForm({
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
        this.clientStore.deleteGrantType(this.client.clientId, this.grantTypeToDelete.type).subscribe(_ => {
            this.toast.showSuccess(`Grant type '${this.selectedGrantType}' was successfully removed from the client.`);
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
            if (!this.grantTypeStateMatrixService.canGoTo(type, grantType)) {
                result = false;
                break;
            }
        }
        return result;
    }

    public showDeleteAlert(grantType: string): void {
        this.grantTypeToDelete = this.rows.find(x => x.type === grantType);
        setTimeout(() => this.deleteAlert.fire(), 0);
    }

    public ngOnDestroy(): void {
        if (this.getDataSubscription) {
            this.getDataSubscription.unsubscribe();
        }
    }
}
