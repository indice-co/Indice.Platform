import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo, ClaimInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { NgForm } from '@angular/forms';

@Component({
    selector: 'app-client-grant-types',
    templateUrl: './client-grant-types.component.html'
})
export class ClientGrantTypesComponent implements OnInit, OnDestroy {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('grantTypesform', { static: false }) private _form: NgForm;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, public _toast: ToastService) { }

    public client: SingleClientInfo;
    public columns: TableColumn[] = [];
    public selectedGrantType = '';
    public rows: { type: string }[];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'type', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        const clientId = this._route.parent.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
            this.rows = this.client.grantTypes.map((value: string) => {
                return {
                    type: value
                };
            });
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
