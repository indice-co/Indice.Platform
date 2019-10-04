import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { TableColumn } from '@swimlane/ngx-datatable';
import { UserClientInfo } from 'src/app/core/services/identity-api.service';
import { UserStore } from '../user-store.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
    selector: 'app-user-applications',
    templateUrl: './user-applications.component.html'
})
export class UserApplicationsComponent implements OnInit, OnDestroy {
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    private _getDataSubscription: Subscription;

    constructor(private userStore: UserStore, private route: ActivatedRoute, private modalService: NgbModal) { }

    public columns: TableColumn[] = [];
    public rows: UserClientInfo[] = [];
    public selectedUserClient: UserClientInfo;

    public ngOnInit(): void {
        this.columns = [
            { prop: 'clientId', name: 'App Id', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'clientName', name: 'App Name', draggable: false, canAutoResize: true, sortable: true, resizeable: false },
            { prop: 'description', name: 'Description', draggable: false, canAutoResize: true, sortable: false, resizeable: false },
            { prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' }
        ];
        const userId = this.route.parent.snapshot.params.id;
        this._getDataSubscription = this.userStore.getUserApplications(userId).subscribe((userApplications: UserClientInfo[]) => {
            this.rows = userApplications;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public showDetails(client: UserClientInfo, content: any): void {
        this.selectedUserClient = client;
        this.modalService.open(content);
    }
}
