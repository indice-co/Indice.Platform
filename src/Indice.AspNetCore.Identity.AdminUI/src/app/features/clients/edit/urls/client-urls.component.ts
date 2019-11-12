import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgForm } from '@angular/forms';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { ClientUrl } from './models/client-url';
import { UrlType } from './models/urlType';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';

@Component({
    selector: 'app-client-urls',
    templateUrl: './client-urls.component.html'
})
export class ClientUrlsComponent implements OnInit, OnDestroy {
    @ViewChild('checkboxTemplate', { static: true }) private _checkboxTemplate: TemplateRef<HTMLElement>;
    @ViewChild('redirectTemplate', { static: true }) private _redirectTemplate: TemplateRef<HTMLElement>;
    @ViewChild('corsTemplate', { static: true }) private _corsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('postLogoutRedirectTemplate', { static: true }) private _postLogoutRedirectTemplate: TemplateRef<HTMLElement>;
    @ViewChild('urlForm', { static: false }) private _form: NgForm;
    private _getDataSubscription: Subscription;
    private _updateClientUrlsSubscription: Subscription;
    private _clientId: string;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore, private _toast: ToastService, private _utilities: UtilitiesService) { }

    public columns: TableColumn[] = [];
    public rows: ClientUrl[] = [];
    public client: SingleClientInfo;
    public url: string;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'url', name: 'URL', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'isRedirect', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate, headerTemplate: this._redirectTemplate },
            { prop: 'isCors', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate, headerTemplate: this._corsTemplate },
            { prop: 'isPostLogoutRedirect', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate, headerTemplate: this._postLogoutRedirectTemplate }
        ];
        this._clientId = this._route.parent.snapshot.params.id;
        this.renderTable();
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._updateClientUrlsSubscription) {
            this._updateClientUrlsSubscription.unsubscribe();
        }
    }

    public checkboxChanged(event: any, column: any, row: ClientUrl) {
        const checked = event.currentTarget.checked;
        const property = column.prop;
        this._clientStore.updateClientUrl(this._clientId, row.url, checked, property === 'isRedirect' ? UrlType.Redirect : (property === 'isCors' ? UrlType.Cors : UrlType.PostLogoutRedirect));
        setTimeout(() => this.renderTable(), 0);
    }

    public addClientUrl() {
        this._clientStore.updateClientUrl(this._clientId, this.url, true, UrlType.Cors);
        setTimeout(() => this.renderTable(), 0);
        this._form.resetForm({
            url: ''
        });
    }

    public update(): void {
        this._updateClientUrlsSubscription = this._clientStore.sendUpdateClientUrls(this._clientId, this.client.allowedCorsOrigins, this.client.postLogoutRedirectUris, this.client.redirectUris).subscribe(_ => {
            this._toast.showSuccess(`Client URLs were successfully updated.`);
        });
    }

    private renderTable(): void {
        this._getDataSubscription = this._clientStore.getClient(this._clientId).subscribe((client: SingleClientInfo) => {
            this.client = client;
            let urls = client.redirectUris;
            if (client.allowedCorsOrigins) {
                urls = urls.concat(client.allowedCorsOrigins);
            }
            if (client.postLogoutRedirectUris) {
                urls = urls.concat(client.postLogoutRedirectUris);
            }
            urls = [...new Set(urls)];
            const rows = [];
            urls.forEach((value: string) => {
                rows.push({
                    url: value,
                    isCors: client.allowedCorsOrigins.includes(value),
                    isPostLogoutRedirect: client.postLogoutRedirectUris.includes(value),
                    isRedirect: client.redirectUris.includes(value),
                    id: this._utilities.newGuid()
                } as ClientUrl);
            });
            this.rows = rows;
        });
    }
}
