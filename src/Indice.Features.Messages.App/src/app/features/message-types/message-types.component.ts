import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesApiClient, MessageType, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-message-types',
    templateUrl: './message-types.component.html'
})
export class MessageTypesComponent extends BaseListComponent<MessageType> implements OnInit {
    constructor(
        route: ActivatedRoute,
        router: Router,
        private _api: MessagesApiClient
    ) {
        super(route, router);
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

    public actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
