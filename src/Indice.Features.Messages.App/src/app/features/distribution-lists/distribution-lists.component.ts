import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TenantService } from '@indice/ng-auth';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { DistributionList, DistributionListResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-distribution-lists',
    templateUrl: './distribution-lists.component.html'
})
export class DistributionListsComponent extends BaseListComponent<DistributionList> implements OnInit {
    constructor(
        route: ActivatedRoute,
        private _router: Router,
        private _api: MessagesApiClient,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService,
        private _tenantService: TenantService
    ) {
        super(route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'name';
        this.sortdir = 'asc';
        this.search = '';
        this.sortOptions = [new MenuOption('Όνομα', 'name')];
    }

    public newItemLink: string | null = 'create-distribution-list';
    public full = true;

    public ngOnInit(): void {
        super.ngOnInit();
    }

    public loadItems(): Observable<IResultSet<DistributionList> | null | undefined> {
        return this._api
            .getDistributionLists(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: DistributionListResultSet) => (result as IResultSet<DistributionList>)));
    }

    public deleteConfirmation(list: DistributionList): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: 'Διαγραφή',
                message: `Είστε σίγουρος ότι θέλετε να διαγράψετε τη λίστα ${list.name};`,
                data: list
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteDistributionList(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Η λίστα με όνομα '${response.result.data.name}' διαγράφηκε με επιτυχία.`);
                    const navigationCommands = ['distribution-lists'];
                    const tenantAlias = this._tenantService.getTenantValue();
                    if (tenantAlias !== '') {
                        navigationCommands.unshift(tenantAlias);
                    }
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(navigationCommands));
                });
            }
        });
    }

    public actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
