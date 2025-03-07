import { Component, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { DeviceInfo } from 'src/app/core/services/identity-api.service';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { UserStore } from '../user-store.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';

@Component({
    selector: 'app-user-devices',
    templateUrl: './user-devices.component.html'
})
export class UserDevicesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription | undefined;
    private _deviceToDelete: DeviceInfo;
    private _userId: string;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('userDeviceList', { static: true }) public _userDeviceList: ListViewComponent;
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;

    constructor(
        private _userStore: UserStore,
        private _route: ActivatedRoute,
        private _toast: ToastService
    ) { }

    public columns: TableColumn[] = [];
    public rows: DeviceInfo[] = [];

    public ngOnInit(): void { 
        this.columns = [
            { prop: 'deviceId', name: 'Device Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.keyTemplate },
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'model', name: 'Model', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'osVersion', name: 'OS Version', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'clientType', name: 'Client Type', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.keyTemplate },
            { prop: 'platform', name: 'Platform', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'dateCreated', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.dateTimeTemplate },
            { prop: 'lastSignInDate', name: 'Last Sign In', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.dateTimeTemplate },
            { prop: 'isPushNotificationsEnabled', name: 'Push Enabled', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'isTrusted', name: 'Trusted', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'supportsFingerprintLogin', name: 'Fingerprint', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'supportsPinLogin', name: '4Pin', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'deviceId', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center'}
        ];
        this._userId = this._route.parent.snapshot.params['id'];
        this._getDataSubscription = this._userStore.getUserDevices(this._userId).subscribe((userDevices: DeviceInfo[]) => this.rows = userDevices);
    }

    public showDeleteAlert(key: string): void {
        this._deviceToDelete = this.rows.find(x => x.deviceId === key);
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._userStore.deleteUserDevice(this._userId, this._deviceToDelete.deviceId).subscribe(_ => {
            this.rows = [...this.rows.filter(x => x.deviceId !== this._deviceToDelete.deviceId)];
            this._toast.showSuccess(`Login provider '${this._deviceToDelete.name}' was successfully removed from user.`);
        });
    }

    public ngOnDestroy(): void {
        this._getDataSubscription?.unsubscribe();
    }
}
