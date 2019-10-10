import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ClientStore } from '../client-store.service';
import { ClientUrl } from './models/client-url';
import { UrlType } from './models/urlType';

@Component({
    selector: 'app-client-urls',
    templateUrl: './client-urls.component.html'
})
export class ClientUrlsComponent implements OnInit, OnDestroy {
    @ViewChild('checkboxTemplate', { static: true }) private _checkboxTemplate: TemplateRef<HTMLElement>;
    private _getDataSubscription: Subscription;
    private _clientId: string;

    constructor(private _route: ActivatedRoute, private _clientStore: ClientStore) { }

    public columns: TableColumn[] = [];
    public rows: ClientUrl[] = [];

    public ngOnInit(): void {
        this.columns = [
            { prop: 'url', name: 'URL', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'isRedirect', name: 'Redirect', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate },
            { prop: 'isCors', name: 'CORS', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate },
            { prop: 'isPostLogoutRedirect', name: 'Post Logout Redirect', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._checkboxTemplate }
        ];
        this._clientId = this._route.parent.snapshot.params.id;
        this.renderTable();
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public checkboxChanged(event: any, column: any, row: ClientUrl) {
        const checked = event.currentTarget.checked;
        const property = column.prop;
        this._clientStore.updateClientUrl(this._clientId, row.url, checked, property === 'isRedirect' ? UrlType.Redirect : (property === 'isCors' ? UrlType.Cors : UrlType.PostLogoutRedirect));
        setTimeout(() => this.renderTable(), 0);
    }

    public update(): void { }

    private renderTable(): void {
        this._getDataSubscription = this._clientStore.getClient(this._clientId).subscribe((client: SingleClientInfo) => {
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
                    isRedirect: client.redirectUris.includes(value)
                } as ClientUrl);
            });
            this.rows = rows;
        });
    }
}
