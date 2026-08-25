import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { IdentityApiService, CreateApiScopeRequest, ApiScopeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-scope-add',
    templateUrl: './scope-add.component.html',
    standalone: false
})
export class ScopeAddComponent implements OnInit {
    public scope: CreateApiScopeRequest;

    constructor(
        private _api: IdentityApiService,
        private _toast: ToastService,
        private _router: Router
    ) { }

    public ngOnInit(): void {
        this.scope = new CreateApiScopeRequest({
            name: '',
            displayName: '',
            description: '',
            required: false,
            emphasize: false,
            showInDiscoveryDocument: true,
            userClaims: [],
            translations: {}
        });
    }

    public submit(): void {
        this._api.createApiScope(this.scope).subscribe((createdScope: ApiScopeInfo) => {
            this._toast.showSuccess(`Scope '${createdScope.name}' was created successfully.`);
            this._router.navigate([`app/resources/scopes/${createdScope.name}/details`]);
        });
    }

    public cancel(): void {
        this._router.navigate(['app/resources/scopes']);
    }
}
