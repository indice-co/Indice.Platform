import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { ApiResourceInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-api-resource-details',
    templateUrl: './api-resource-details.component.html'
})
export class ApiResourceDetailsComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute/*, private _clientStore: ClientStore*/, public _toast: ToastService, private _router: Router) { }

    public apiResource: ApiResourceInfo;

    public ngOnInit(): void {
        const clientId = this._route.parent.snapshot.params.id;
        // this._getDataSubscription = this._clientStore.getClient(clientId).subscribe((client: SingleClientInfo) => {
        //     this.client = client;
        // });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public delete(): void {
        // this._clientStore.deleteClient(this.client.clientId).subscribe(_ => {
        //     this._toast.showSuccess(`Client '${this.client.clientName}' was deleted successfully.`);
        //     this._router.navigate(['../../'], { relativeTo: this._route });
        // });
    }

    public update(): void { }
}
