import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { map, Observable } from 'rxjs';
import { Contact, ContactResultSet, DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-distribution-list-contacts',
    templateUrl: './distribution-list-contacts.component.html'
})
export class DistributionListContactsComponent extends BaseListComponent<Contact> implements OnInit {
    private _distributionListId: string = '';

    constructor(
        route: ActivatedRoute,
        private _router: Router,
        private _api: MessagesApiClient,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService,
        private _activatedRoute: ActivatedRoute
    ) {
        super(route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'updatedAt';
        this.sortdir = 'asc';
        this.search = '';
        this.sortOptions = [new MenuOption('Όνομα', 'name')];
    }

    public newItemLink: string | null = 'create-distribution-list-contact';
    public full = true;
    public distributionList = new DistributionList({ name: '' });

    public ngOnInit(): void {
        this._distributionListId = this._activatedRoute.snapshot.params['distributionListId'];
        super.ngOnInit();

    }

    public loadItems(): Observable<IResultSet<Contact> | null | undefined> {
        return this._api
            .getDistributionListContacts(this._distributionListId, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: ContactResultSet) => (result as IResultSet<Contact>)));
    }

    public deleteConfirmation(list: DistributionList): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: `Είστε σίγουρος ότι θέλετε να διαγράψετε τη λίστα ${list.name};`,
                data: list
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteDistributionList(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Η λίστα με όνομα '${response.result.data.name}' διαγράφηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists']));
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
