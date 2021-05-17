import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, RoleInfo, UpdateRoleRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-role-edit',
    templateUrl: './role-edit.component.html'
})
export class RoleEditComponent implements OnInit {
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;

    constructor(
        private _api: IdentityApiService, 
        private _router: Router, 
        private _route: ActivatedRoute, 
        public _toast: ToastService,
        private _authService: AuthService
    ) { }

    public role: RoleInfo = new RoleInfo();
    public canEditRole: boolean;

    public ngOnInit(): void {
        this.canEditRole = this._authService.isAdminUIUsersWriter();
        this.role = this._route.snapshot.data.role;
    }

    public deletePrompt(): void {
        this._deleteAlert.fire();
    }

    public delete(): void {
        this._api.deleteRole(this.role.id).subscribe(_ => {
            this._toast.showSuccess(`Role '${this.role.name}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        this._api.updateRole(this.role.id, {
            description: this.role.description
        } as UpdateRoleRequest).subscribe((response: RoleInfo) => {
            this._toast.showSuccess(`Role '${response.name}' was updated successfully.`);
        });
    }
}
