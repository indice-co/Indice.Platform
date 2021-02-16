import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { IdentityApiService, AppSettingInfo, UpdateAppSettingRequest } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';

@Component({
    selector: 'app-setting-edit',
    templateUrl: './setting-edit.component.html'
})
export class SettingEditComponent implements OnInit {
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;

    constructor(private _api: IdentityApiService, private _router: Router, private _route: ActivatedRoute, public _toast: ToastService) { }

    public setting: AppSettingInfo = new AppSettingInfo();

    public ngOnInit(): void {
        this.setting = this._route.snapshot.data.setting;
    }

    public deletePrompt(): void {
        this._deleteAlert.fire();
    }

    public delete(): void {
        this._api.deleteSetting(this.setting.key).subscribe(_ => {
            this._toast.showSuccess(`Setting '${this.setting.key}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        this._api.updateSetting(this.setting.key, {
            value: this.setting.value
        } as UpdateAppSettingRequest).subscribe((response: AppSettingInfo) => {
            this._toast.showSuccess(`Setting '${response.key}' was updated successfully.`);
        });
    }
}
