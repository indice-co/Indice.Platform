import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { Contact, ContactResultSet, DistributionList, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-distribution-list-contacts',
    templateUrl: './distribution-list-contacts.component.html'
})
export class DistributionListContactsComponent extends BaseListComponent<Contact> implements OnInit, OnDestroy {
    private _distributionListId: string = '';
    private _getListSubscription!: Subscription;

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
        this.sortOptions = [
            new MenuOption('Όνομα', 'firstName'),
            new MenuOption('Επίθετο', 'lastName'),
            new MenuOption('Email', 'email'),
            new MenuOption('Δημιουργήθηκε', 'updatedAt')
        ];
    }

    public newItemLink: string | null = 'create-contact';
    public full = true;
    public distributionList = new DistributionList({ name: '' });

    public ngOnInit(): void {
        this._distributionListId = this._activatedRoute.parent?.snapshot.params['distributionListId'];
        super.ngOnInit();
        this._getListSubscription = this._api.getDistributionListById(this._distributionListId).subscribe((list: DistributionList) => {
            this.distributionList = list;
        });
    }

    public loadItems(): Observable<IResultSet<Contact> | null | undefined> {
        return this._api
            .getDistributionListContacts(this._distributionListId, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: ContactResultSet) => (result as IResultSet<Contact>)));
    }

    public deleteConfirmation(contact: Contact): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: 'Διαγραφή',
                message: `Είστε σίγουρος ότι θέλετε να διαγράψετε την επαφή '${contact.fullName || contact.email}' από τη λίστα '${this.distributionList.name}';`,
                data: contact
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                const contact = response.result.data;
                this._api.removeContactFromDistributionList(this._distributionListId, contact.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Η επαφή '${contact.fullName || contact.email}' αφαιρέθηκε από τη λίστα.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['distribution-lists', this._distributionListId, 'contacts']));
                });
            }
        });
    }

    public ngOnDestroy(): void {
        this._getListSubscription?.unsubscribe();
    }

    public actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
