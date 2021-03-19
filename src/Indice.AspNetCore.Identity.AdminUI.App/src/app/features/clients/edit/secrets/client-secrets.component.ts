import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { saveAs } from 'file-saver';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { CreateSecretRequest, SingleClientInfo, ClientSecretInfo, IdentityApiService, FileResponse } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';

@Component({
    selector: 'app-client-secrets',
    templateUrl: './client-secrets.component.html'
})
export class ClientSecretsComponent implements OnInit, OnDestroy {
    @ViewChild('clientSecretsList', { static: true }) private _clientSecretsList: ListViewComponent;
    @ViewChild('wrapContentTemplate', { static: true }) private _wrapContentTemplate: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;
    @ViewChild('form', { static: false }) private _form: NgForm;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;
    private _clientId: string;

    constructor(private _utilities: UtilitiesService, private _route: ActivatedRoute, private _clientStore: ClientStore, public _toast: ToastService, private _api: IdentityApiService) { }

    public columns: TableColumn[] = [];
    public rows: ClientSecretInfo[] = [];
    public client: SingleClientInfo;
    public secretToDelete: ClientSecretInfo;
    public clientSecret: CreateSecretRequest = new CreateSecretRequest();
    public selectedSecretType = 'SharedSecret';
    public fileToUpload: File;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'value', name: 'Value', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._wrapContentTemplate },
            { prop: 'expiration', name: 'Expiration', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._clientSecretsList.dateTimeTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        this._clientId = this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._clientStore.getClient(this._clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
            this.rows = client.secrets;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public generateValue(): void {
        this.clientSecret.value = this._utilities.newGuid();
    }

    public update(): void {
        if (this.selectedSecretType === 'SharedSecret') {
            this.saveSharedSecret();
        } else if (this.selectedSecretType === 'X509CertificateBase64') {
            this.uploadCertificate();
        }
    }

    public downloadCertificate(clientSecretId: number): void {
        this._api.getCertificate(this.client.clientId, clientSecretId).subscribe((file: FileResponse) => {
            const blob = file.data;
            saveAs(blob, file.fileName);
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

    public certificateSelected(files: FileList): void {
        this.fileToUpload = files[0];
    }

    private saveSharedSecret(): void {
        if (!this.clientSecret.value) {
            return;
        }
        const expiration = (this.clientSecret as any).expiration as NgbDateStruct;
        this.clientSecret.expiration = expiration ? new Date(expiration.year, expiration.month - 1, expiration.day) : undefined;
        this._clientStore.addSharedSecret(this._clientId, this.clientSecret).subscribe(_ => {
            this.rows = [...this.client.secrets];
            this._form.resetForm({
                'client-secret-type': 'SharedSecret',
                'client-secret-value': '',
                'expiration-date': '',
                description: ''
            });
            this._toast.showSuccess(`Secret was successfully added to client '${this.client.clientName}'.`);
        });
    }

    private uploadCertificate(): void {
        if (!this.fileToUpload) {
            return;
        }
        this._clientStore.addCertificate(this._clientId, this.fileToUpload).subscribe(_ => {
            this.rows = [...this.client.secrets];
            this._form.resetForm({ 'client-secret-type': 'X509CertificateBase64' });
            this._toast.showSuccess(`Certificate was uploaded successfully for client '${this.client.clientName}'.`);
        });
    }
}
