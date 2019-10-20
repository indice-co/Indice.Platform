import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { NgbPanelChangeEvent } from '@ng-bootstrap/ng-bootstrap';
import { Subscription } from 'rxjs';
import { ApiResourceInfo, ScopeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../api-resource-store.service';

@Component({
    selector: 'app-api-resource-scopes',
    templateUrl: './api-resource-scopes.component.html'
})
export class ApiResourceScopesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore, public _toast: ToastService, private _router: Router) { }

    public apiResource: ApiResourceInfo;
    public activeScope: ScopeInfo;

    public ngOnInit(): void {
        const apiResourceId = +this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._apiResourceStore.getApiResource(apiResourceId).subscribe((apiResource: ApiResourceInfo) => {
            this.apiResource = apiResource;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public deletePrompt(scope: ScopeInfo): void {
        this.activeScope = scope;
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        // this._apiResourceStore.deleteClient(this.client.clientId).subscribe(_ => {
        //     this._toast.showSuccess(`Client '${this.client.clientName}' was deleted successfully.`);
        //     this._router.navigate(['../../'], { relativeTo: this._route });
        // });
    }

    public update(scope: ScopeInfo): void { }

    public panelChanged(event: NgbPanelChangeEvent): void { }
}
