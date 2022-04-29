import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesApiClient, MessageType, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-message-types',
    templateUrl: './message-types.component.html'
})
export class MessageTypesComponent extends BaseListComponent<MessageType> implements OnInit {
    constructor(
        route: ActivatedRoute,
        private _router: Router,
        private _api: MessagesApiClient,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService
    ) {
        super(route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'name';
        this.sortdir = 'asc';
        this.search = '';
        this.sortOptions = [new MenuOption('Όνομα', 'name')];
    }

    public newItemLink: string | null = 'create-message-type';
    public full = true;

    public ngOnInit(): void {
        super.ngOnInit();
    }

    public loadItems(): Observable<IResultSet<MessageType> | null | undefined> {
        return this._api
            .getMessageTypes(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: MessageTypeResultSet) => (result as IResultSet<MessageType>)));
    }

    public deleteConfirmation(type: MessageType): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: `Είστε σίγουρος ότι θέλετε να διαγράψετε τον τύπο ${type.name};`,
                data: type
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteMessageType(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Ο τύπος με όνομα '${response.result.data.name}' διαγράφηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['message-types']));
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
