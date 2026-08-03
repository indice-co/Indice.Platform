import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { IdentityApiService, ApiScopeInfo, UpdateApiScopeRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-scope-details',
    templateUrl: './scope-details.component.html',
    standalone: false
})
export class ScopeDetailsComponent implements OnInit {
    public scope: ApiScopeInfo | undefined;
    public scopeName: string;
    public canEdit: boolean;

    constructor(
        private _route: ActivatedRoute,
        private _api: IdentityApiService,
        private _toast: ToastService,
        private _authService: AuthService
    ) { }

    public ngOnInit(): void {
        this.canEdit = this._authService.isAdminUIClientsWriter();
        this._route.parent?.params.subscribe(params => {
            this.scopeName = params['name'];
            this.loadScope();
        });
    }

    public submit(): void {
        if (!this.scope) {
            return;
        }
        const request = new UpdateApiScopeRequest({
            displayName: this.scope.displayName,
            description: this.scope.description,
            required: this.scope.emphasize,
            emphasize: this.scope.emphasize,
            showInDiscoveryDocument: this.scope.showInDiscoveryDocument,
            translations: {}
        });
        this._api.updateApiScope(this.scopeName, request).subscribe(() => {
            this._toast.showSuccess(`Scope '${this.scopeName}' was updated successfully.`);
        });
    }

    private loadScope(): void {
        this._api.getApiScopes(1, 1000).subscribe(result => {
            this.scope = result.items?.find(s => s.name === this.scopeName);
        });
    }
}
