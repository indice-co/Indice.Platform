import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { TableColumn } from '@swimlane/ngx-datatable';
import { Subscription } from 'rxjs';
import { DeviceInfo } from 'src/app/core/services/identity-api.service';
import { ListViewComponent } from 'src/app/shared/components/list-view/list-view.component';
import { UserStore } from '../user-store.service';

@Component({
    selector: 'app-user-devices',
    templateUrl: './user-devices.component.html'
})
export class UserDevicesComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    @ViewChild('userDeviceList', { static: true }) public _userDeviceList: ListViewComponent;

    constructor(
        private _userStore: UserStore,
        private _route: ActivatedRoute
    ) { }

    public columns: TableColumn[] = [];
    public rows: DeviceInfo[] = [];

    public ngOnInit(): void { 
        this.columns = [
            { prop: 'deviceId', name: 'Device Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'name', name: 'Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'model', name: 'Model', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'osVersion', name: 'OS Version', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'clientType', name: 'Client Type', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'platform', name: 'Platform', draggable: false, canAutoResize: false, sortable: true, resizeable: false },
            { prop: 'dateCreated', name: 'Created At', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.dateTimeTemplate },
            { prop: 'lastSignInDate', name: 'Last Sign In', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.dateTimeTemplate },
            { prop: 'isPushNotificationsEnabled', name: 'Push Enabled', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'isTrusted', name: 'Trusted', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'supportsFingerprintLogin', name: 'Fingerprint', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            { prop: 'supportsPinLogin', name: '4Pin', draggable: false, canAutoResize: false, sortable: true, resizeable: false, cellTemplate: this._userDeviceList.booleanTemplate },
            // { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        const userId = this._route.parent.snapshot.params.id;
        this._getDataSubscription = this._userStore.getUserDevices(userId).subscribe((userDevices: DeviceInfo[]) => this.rows = userDevices);
    }

    public ngOnDestroy(): void {
        this._getDataSubscription?.unsubscribe();
    }
}
