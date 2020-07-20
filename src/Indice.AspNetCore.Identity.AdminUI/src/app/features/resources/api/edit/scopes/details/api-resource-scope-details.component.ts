import { Component, OnInit, OnDestroy, ViewChild, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { ApiScopeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../../../api-resource-store.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';

@Component({
    selector: 'app-api-resource-scope-details',
    templateUrl: './api-resource-scope-details.component.html'
})
export class ApiResourceScopeDetailsComponent implements OnInit, OnDestroy {
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;
    private _deleteApiResourceSubscription: Subscription;
    private _updateApiResourceSubscription: Subscription;
    private _apiResourceId: number;

    constructor(private _route: ActivatedRoute, private _apiResourceStore: ApiResourceStore, public _toast: ToastService, public utilities: UtilitiesService) { }

    @Input() public scope = new ApiScopeInfo();
    @Input() public editable = false;
    public discriminator = this.utilities.newGuid();

    public ngOnInit(): void {
        this._apiResourceId = +this._route.parent.snapshot.params.id;
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._deleteApiResourceSubscription) {
            this._deleteApiResourceSubscription.unsubscribe();
        }
        if (this._updateApiResourceSubscription) {
            this._updateApiResourceSubscription.unsubscribe();
        }
    }

    public deletePrompt(scope: ApiScopeInfo): void {
        this.scope = scope;
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._deleteApiResourceSubscription = this._apiResourceStore.deleteApiResourceScope(this._apiResourceId, this.scope.id).subscribe(_ => {
            this._toast.showSuccess(`API scope '${this.scope.name}' was deleted successfully.`);
        });
    }

    public update(): void {
        this._updateApiResourceSubscription = this._apiResourceStore.updateApiResourceScope(this._apiResourceId, this.scope).subscribe(_ => {
            this._toast.showSuccess(`API scope '${this.scope.name}' was updated successfully.`);
        });
    }
}
