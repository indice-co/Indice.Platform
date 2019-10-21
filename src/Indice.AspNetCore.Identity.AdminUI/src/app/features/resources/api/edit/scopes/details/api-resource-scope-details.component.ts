import { Component, OnInit, OnDestroy, ViewChild, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { NgbPanelChangeEvent } from '@ng-bootstrap/ng-bootstrap';
import { ScopeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../../api-resource-store.service';

@Component({
    selector: 'app-api-resource-scope-details',
    templateUrl: './api-resource-scope-details.component.html'
})
export class ApiResourceScopeDetailsComponent implements OnInit, OnDestroy {
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore, public _toast: ToastService, private _router: Router) { }

    @Input() public scope = new ScopeInfo();

    public ngOnInit(): void { }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public deletePrompt(scope: ScopeInfo): void {
        this.scope = scope;
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
