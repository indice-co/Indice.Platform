import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgForm } from '@angular/forms';

import { TableColumn } from '@swimlane/ngx-datatable';
import { of, pipe, Subscription } from 'rxjs';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { ClientUrl } from './models/client-url';
import { UrlType } from './models/urlType';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { AuthService } from 'src/app/core/services/auth.service';
import { map } from 'rxjs/operators';

@Component({
    selector: 'app-client-urls',
    templateUrl: './client-urls.component.html'
})
export class ClientUrlsComponent implements OnInit, OnDestroy {
    @ViewChild('checkboxTemplate', { static: true }) private _checkboxTemplate: TemplateRef<HTMLElement>;
    @ViewChild('redirectTemplate', { static: true }) private _redirectTemplate: TemplateRef<HTMLElement>;
    @ViewChild('corsTemplate', { static: true }) private _corsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('postLogoutRedirectTemplate', { static: true }) private _postLogoutRedirectTemplate: TemplateRef<HTMLElement>;
    @ViewChild('urlTemplate', { static: true }) private _urlTemplate: TemplateRef<HTMLElement>;
    @ViewChild('urlForm', { static: false }) private _form: NgForm;
    private _getDataSubscription: Subscription;
    private _updateClientUrlsSubscription: Subscription;
    private _clientId: string;

    constructor(
        private route: ActivatedRoute,
        private clientStore: ClientStore,
        private toast: ToastService,
        private utilities: UtilitiesService,
        private authService: AuthService
    ) { }

    public columns: TableColumn[] = [];
    public rows: ClientUrl[] = [];
    public client: SingleClientInfo;
    public url: string;
    public canEditClient: boolean;
    public urlPattern = /(?:^|\s)((https?:\/\/)?(?:localhost|[\w-]+(?:\.[\w-]+)+)(:\d+)?(\/\S*)?)/;

    public ngOnInit(): void {
        this.canEditClient = this.authService.isAdminUIClientsWriter();
        this.columns = [
            { prop: 'url', name: 'URL', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._urlTemplate },
            { prop: 'isRedirect', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate, headerTemplate: this._redirectTemplate },
            { prop: 'isCors', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate, headerTemplate: this._corsTemplate },
            { prop: 'isPostLogoutRedirect', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate, headerTemplate: this._postLogoutRedirectTemplate }
        ];
        this._clientId = this.route.parent.snapshot.params['id'];
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
        this.clientStore.updateClientUrl(this._clientId, row.url, checked, property === 'isRedirect' ? UrlType.Redirect : (property === 'isCors' ? UrlType.Cors : UrlType.PostLogoutRedirect));
        setTimeout(() => this.renderTable(), 0);
    }

    public addClientUrl() {
        this.clientStore.updateClientUrl(this._clientId, this.url, true, UrlType.Cors);
        setTimeout(() => this.renderTable(), 0);
        this._form.resetForm({ url: '' });
    }

    public update(): void {
        this._updateClientUrlsSubscription = this
            .clientStore
            .sendUpdateClientUrls(this._clientId, this.client.allowedCorsOrigins, this.client.postLogoutRedirectUris, this.client.redirectUris)
            .pipe(map(_ => {
                this.client['deletingUrls'] = [];
                setTimeout(() => this.renderTable(), 0);
            }))
            .subscribe(_ => {
                this.toast.showSuccess(`Client URLs were successfully updated.`);
            });
    }

    public isDeletedUrl(url: string): boolean {
        return this.client['deletingUrls'] ? (this.client['deletingUrls'] as string[]).indexOf(url) > -1 : false;
    }

    public isInvalidCorsOrigin(url: string): boolean {
        const isCorsOrigin = this.client.allowedCorsOrigins.indexOf(url) > -1;
        if (!isCorsOrigin) {
            return false;
        }
        const originPattern = /^https?:\/\/(?=.{1,254}(?::|$))(?:(?!\d|-)(?![a-z0-9\-]{1,62}-(?:\.|:|$))[a-z0-9\-]{1,63}\b(?!\.$)\.?)+(:\d+)?$/i;
        return !originPattern.test(url);
    }

    public hasErrors(): boolean {
        return this.client.allowedCorsOrigins.some(x => this.isInvalidCorsOrigin(x));
    }

    private renderTable(): void {
        this._getDataSubscription = this
            .clientStore
            .getClient(this._clientId)
            .subscribe((client: SingleClientInfo) => {
                this.client = client;
                let urls = client.redirectUris;
                if (client.allowedCorsOrigins) {
                    urls = urls.concat(client.allowedCorsOrigins);
                }
                if (client.postLogoutRedirectUris) {
                    urls = urls.concat(client.postLogoutRedirectUris);
                }
                if (client['deletingUrls']) {
                    urls = urls.concat(client['deletingUrls']);
                }
                urls = [...new Set(urls)];
                const rows = [];
                urls.forEach((value: string) => {
                    rows.push({
                        url: value,
                        isCors: client.allowedCorsOrigins.includes(value),
                        isPostLogoutRedirect: client.postLogoutRedirectUris.includes(value),
                        isRedirect: client.redirectUris.includes(value),
                        id: this.utilities.newGuid()
                    } as ClientUrl);
                });
                this.rows = rows;
            });
    }
}
