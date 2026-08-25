import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { IdentityApiService, ApiScopeInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-scope-edit',
    templateUrl: './scope-edit.component.html',
    standalone: false
})
export class ScopeEditComponent implements OnInit {
    public scopeName: string;
    public scope: ApiScopeInfo | undefined;
    public canEdit: boolean = false;

    constructor(
        private _route: ActivatedRoute,
        private _router: Router,
        private _api: IdentityApiService,
        private _toast: ToastService
    ) { }

    public ngOnInit(): void {
        this._route.params.subscribe(params => {
            this.scopeName = params['name'];
            this.loadScope();
        });
    }

    public deleteScope(): void {
        if (confirm(`Are you sure you want to delete scope '${this.scopeName}'?`)) {
            this._api.deleteApiScope(this.scopeName).subscribe(() => {
                this._toast.showSuccess(`Scope '${this.scopeName}' was deleted successfully.`);
                this._router.navigate(['app/resources/scopes']);
            });
        }
    }

    private loadScope(): void {
        // Since there's no single scope endpoint, we'll load all scopes and filter
        // In a production scenario, you might want to add a specific endpoint
        this._api.getApiScopes(1, 1000).subscribe(result => {
            this.scope = result.items?.find(s => s.name === this.scopeName);
            if (!this.scope) {
                this._toast.showDanger(`Scope '${this.scopeName}' not found.`);
                this._router.navigate(['app/resources/scopes']);
            }
        });
    }
}
