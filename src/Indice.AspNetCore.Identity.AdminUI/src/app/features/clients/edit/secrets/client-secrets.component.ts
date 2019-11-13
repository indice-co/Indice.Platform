import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { SecretType, ApiSecretInfo, CreateSecretRequest, ICreateSecretRequest, SingleClientInfo, ClientSecretInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';

@Component({
    selector: 'app-client-secrets',
    templateUrl: './client-secrets.component.html'
})
export class ClientSecretsComponent implements OnInit, OnDestroy {
    @ViewChild('clientSecretsList', { static: true }) private _clientSecretsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;
    @ViewChild('form', { static: false }) private _form: NgForm;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;
    private _clientId: string;

    constructor(private _utilities: UtilitiesService, private _route: ActivatedRoute, private _clientStore: ClientStore, public _toast: ToastService) { }

    public columns: TableColumn[] = [];
    public rows: ClientSecretInfo[] = [];
    public client: SingleClientInfo;
    public secretToDelete: ClientSecretInfo;
    public apiSecret: CreateSecretRequest = new CreateSecretRequest({
        type: SecretType.SharedSecret
    } as ICreateSecretRequest);

    public ngOnInit(): void {
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'valueText', name: 'Value', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'expiration', name: 'Expiration', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._clientSecretsList.dateTimeTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        this._clientId = this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(this._clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
            if (this.client.secrets) {
                this.client.secrets.forEach((value: ClientSecretInfo) => {
                    (value as any).valueText = 'Value is hidden';
                });
            }
            this.rows = client.secrets;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public generateValue(): void {
        this.apiSecret.value = this._utilities.newGuid();
    }

    public update(): void {
        if (!this.apiSecret.type || !this.apiSecret.value) {
            return;
        }
        const expiration = (this.apiSecret as any).expiration as NgbDateStruct;
        this.apiSecret.expiration = expiration ? new Date(expiration.year, expiration.month - 1, expiration.day) : undefined;
        this._clientStore.addSecret(this._clientId, this.apiSecret).subscribe(_ => {
            this.rows = [...this.client.secrets];
            this._form.resetForm({
                'client-secret-type': SecretType.SharedSecret,
                'client-secret-value': '',
                'expiration-date': '',
                description: ''
            });
            this._toast.showSuccess(`Secret was successfully added to client '${this.client.clientName}'.`);
        });
    }

    public showDeleteAlert(id: number): void {
        this.secretToDelete = this.rows.find(x => x.id === id);
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._clientStore.deleteClientSecret(this._clientId, this.secretToDelete).subscribe(_ => {
            this.rows = [...this.client.secrets];
            this._toast.showSuccess(`Secret was successfully removed from client '${this.client.clientName}'.`);
        });
    }
}
