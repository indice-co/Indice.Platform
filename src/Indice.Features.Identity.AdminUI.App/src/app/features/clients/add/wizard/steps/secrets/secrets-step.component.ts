import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';

import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { TableColumn } from '@swimlane/ngx-datatable';
import { StepBaseComponent } from '../../../../../../shared/components/step-base/step-base.component';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { CreateSecretRequest, FileParameter, IdentityApiService, ProblemDetails, SecretInfoBase, HttpValidationProblemDetails } from 'src/app/core/services/identity-api.service';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { ClientWizardModel } from '../../models/client-wizard-model';
import { ValidationSummaryComponent } from 'src/app/shared/components/validation-summary/validation-summary.component';

@Component({
    selector: 'app-secrets-step',
    templateUrl: './secrets-step.component.html'
})
export class SecretsStepComponent extends StepBaseComponent<ClientWizardModel> implements OnInit {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('wrapContentTemplate', { static: true }) private _wrapContentTemplate: ListViewComponent;
    @ViewChild('clientSecretsList', { static: true }) private _clientSecretsList: ListViewComponent;
    @ViewChild('validationSummary', { static: false }) private _validationSummary: ValidationSummaryComponent;
    private _initialSecrets: CreateSecretRequest[];
    private _certificates: File[] = [];

    constructor(
        private _utilities: UtilitiesService,
        private _api: IdentityApiService
    ) {
        super();
    }

    public columns: TableColumn[] = [];
    public rows: CreateSecretRequest[] = [];
    public clientSecret: CreateSecretRequest = new CreateSecretRequest();
    public selectedSecretType = 'SharedSecret';
    public fileToUpload: File;
    public problemDetails: ProblemDetails;

    public ngOnInit(): void {
        this._initialSecrets = this.data.form.get('secrets').value as CreateSecretRequest[];
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellClass: 'd-flex align-items-center' },
            { prop: 'value', name: 'Value', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._wrapContentTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'expiration', name: 'Expiration', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._clientSecretsList.dateTimeTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._wrapContentTemplate, cellClass: 'd-flex align-items-center' },
            { prop: 'value', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        if (this._initialSecrets.length > 0) {
            this.rows = this._initialSecrets;
        }
    }

    public addClientSecret(): void {
        this.formValidated.emit(true);
        if (this.selectedSecretType === 'SharedSecret') {
            this.addSharedSecret();
        } else if (this.selectedSecretType === 'X509CertificateBase64') {
            this.getCertificateMetadata();
        }
    }

    public certificateSelected(files: FileList): void {
        this.fileToUpload = files[0];
    }

    public removeClientSecret(row: CreateSecretRequest): void {
        const index = this.rows.findIndex(x => x.value === row.value);
        if (index >= -1) {
            this.rows.splice(index, 1);
            this.rows = [...this.rows];
        }
    }

    public generateValue(): void {
        this.clientSecret.value = this._utilities.newGuid();
    }

    public isValid(): boolean {
        return true;
    }

    private addSharedSecret(): void {
        if (!this.clientSecret.value) {
            return;
        }
        const expiration = (this.clientSecret as any).expiration as NgbDateStruct;
        (this.clientSecret as any).type = this.selectedSecretType;
        this.clientSecret.expiration = expiration ? new Date(expiration.year, expiration.month - 1, expiration.day) : undefined;
        this.rows.push(this.clientSecret);
        this.rows = [...this.rows];
        this.data.form.get('secrets').setValue(this.rows);
        this.formValidated.emit(false);
        this.clientSecret = new CreateSecretRequest();
    }

    private getCertificateMetadata(): void {
        if (!this.fileToUpload) {
            return;
        }
        const fileParameter: FileParameter = { data: this.fileToUpload, fileName: this.fileToUpload.name };
        this._api.getCertificateMetadata(fileParameter).subscribe((response: SecretInfoBase) => {
            (this.clientSecret as any).type = this.selectedSecretType;
            this.clientSecret.expiration = response.expiration;
            this.clientSecret.value = response.value;
            this.clientSecret.description = response.description;
            this.rows.push(this.clientSecret);
            this._certificates.push(this.fileToUpload);
            this.rows = [...this.rows];
            this.data.form.get('certificates').setValue([...this._certificates]);
            this.formValidated.emit(false);
            this.clientSecret = new CreateSecretRequest();
        }, (problemDetails: HttpValidationProblemDetails) => {
            this.problemDetails = problemDetails
            setTimeout(() => this._validationSummary.clear(), 5000);
        });
    }
}
