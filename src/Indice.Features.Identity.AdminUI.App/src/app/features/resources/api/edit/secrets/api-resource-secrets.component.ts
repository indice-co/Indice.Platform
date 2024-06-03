import { Component, OnInit, ViewChild, TemplateRef, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgForm } from '@angular/forms';

import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { NgbDateCustomParserFormatter } from 'src/app/shared/services/custom-parser-formatter.service';
import { ApiResourceInfo, ApiSecretInfo, CreateSecretRequest } from 'src/app/core/services/identity-api.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { ApiResourceStore } from '../../api-resource-store.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-api-resource-secrets',
    templateUrl: './api-resource-secrets.component.html',
    providers: [NgbDateCustomParserFormatter]
})
export class ApiResourceSecretsComponent implements OnInit, OnDestroy {
    @ViewChild('apiSecretsList', { static: true }) private _apiSecretsList: ListViewComponent;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('optionalTemplate', { static: true }) private _optionalTemplate: TemplateRef<HTMLElement>;
    @ViewChild('form', { static: false }) private _form: NgForm;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;
    private _apiResourceId: number;

    constructor(
        private _utilities: UtilitiesService,
        private _route: ActivatedRoute,
        private _apiResourceStore: ApiResourceStore,
        private _toast: ToastService,
        private _authService: AuthService
    ) { }

    public columns: TableColumn[] = [];
    public rows: ApiSecretInfo[] = [];
    public apiResource: ApiResourceInfo;
    public secretToDelete: ApiSecretInfo;
    public apiSecret: CreateSecretRequest = new CreateSecretRequest();
    public selectedSecretType = 'SharedSecret';
    public canEditResource: boolean;

    public ngOnInit(): void {
        this.canEditResource = this._authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'valueText', name: 'Value', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'expiration', name: 'Expiration', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._apiSecretsList.dateTimeTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._optionalTemplate, cellClass: 'd-flex align-items-center' }
        ];
        if (this.canEditResource) {
            this.columns.push({ prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' });
        }
        this._apiResourceId = +this._route.parent.snapshot.params['id'];
        this._getDataSubscription = this._apiResourceStore.getApiResource(this._apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            this.apiResource = apiResource;
            if (this.apiResource.secrets) {
                this.apiResource.secrets.forEach((value: ApiSecretInfo) => {
                    (value as any).valueText = 'Value is hidden';
                });
            }
            this.rows = apiResource.secrets;
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
        if (!this.apiSecret.value) {
            return;
        }
        const expiration = (this.apiSecret as any).expiration as NgbDateStruct;
        this.apiSecret.expiration = expiration ? new Date(expiration.year, expiration.month - 1, expiration.day) : undefined;
        this._apiResourceStore.addApiResourceSecret(this._apiResourceId, this.apiSecret).subscribe(_ => {
            this.rows = [...this.apiResource.secrets];
            this._form.resetForm({
                'client-secret-type': 'SharedSecret',
                'client-secret-value': '',
                'expiration-date': '',
                description: ''
            });
            this._toast.showSuccess(`API secret was added successfully.`);
        });
    }

    public showDeleteAlert(id: number): void {
        this.secretToDelete = this.rows.find(x => x.id === id);
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._apiResourceStore.deleteApiResourceSecret(this._apiResourceId, this.secretToDelete).subscribe(_ => {
            this.rows = [...this.apiResource.secrets];
            this._toast.showSuccess(`API secret was deleted successfully.`);
        });
    }
}
