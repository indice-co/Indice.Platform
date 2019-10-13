import { Component, OnInit, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { ClientStore } from '../../client-store.service';
import { SingleClientInfo, ClaimInfo } from 'src/app/core/services/identity-api.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-client-claims',
    templateUrl: './client-claims.component.html'
})
export class ClientClaimsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;

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
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public addClaim(): void { }

    public update(): void { }
}
