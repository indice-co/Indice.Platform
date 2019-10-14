import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo, ClaimInfo } from 'src/app/core/services/identity-api.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { NgForm } from '@angular/forms';

@Component({
    selector: 'app-client-claims',
    templateUrl: './client-claims.component.html'
})
export class ClientClaimsComponent implements OnInit, OnDestroy {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('claimsform', { static: false }) private _form: NgForm;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, public utilities: UtilitiesService, public _toast: ToastService) { }

    public client: SingleClientInfo;
    public columns: TableColumn[] = [];
    public selectedClaimName: string;
    public selectedClaimValue: string;
    public rows: ClaimInfo[];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'value', name: 'Value', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        const clientId = this._route.parent.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
            this.rows = this.client.claims;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public addClaim(): void {
        this._clientStore.addClaim(this.client.clientId, {
            type: this.selectedClaimName,
            value: this.selectedClaimValue
        } as ClaimInfo).subscribe(_ => {
            this._toast.showSuccess(`Claim '${this.selectedClaimName}' was successfully added to the client.`);
            this._form.resetForm({
                'claim-name': '',
                'claim-value': ''
            });
            this.rows = [...this.rows];
        });
    }

    public update(): void { }
}
