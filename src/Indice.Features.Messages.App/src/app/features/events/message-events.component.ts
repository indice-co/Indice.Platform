import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesApiClient, MessageEvent, MessageEventResultSet, MessageChannelKind } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-message-events',
  templateUrl: './message-events.component.html',
  standalone: false
})
export class MessageEventsComponent extends BaseListComponent<MessageEvent> implements OnInit {
  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _api: MessagesApiClient,
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'createdOn';
    this.sortdir = 'desc';
    this.search = '';
    this.sortOptions = [
      new MenuOption('Created On', 'createdOn'),
      new MenuOption('Recipient', 'recipient')];
  }
  public channelTypeFilter: MessageChannelKind[] | undefined = undefined;
  public channelTypeSelectedOption: MessageChannelKind[] | undefined = undefined;
  public newItemLink: string | null = null;
  public override ngOnInit(): void {
    super.ngOnInit();
  }

  public channels: MenuOption[] = [
    new MenuOption(MessageChannelKind.Email, MessageChannelKind.Email),
    new MenuOption(MessageChannelKind.Inbox, MessageChannelKind.Inbox),
    new MenuOption(MessageChannelKind.SMS, MessageChannelKind.SMS),
    new MenuOption(MessageChannelKind.PushNotification, MessageChannelKind.PushNotification)
  ];
  // Match the abstract signature exactly: no | null | undefined, same access level.
  public override loadItems(): Observable<IResultSet<MessageEvent>> {
    return this._api
      .getEventsList(
        this.page,
        this.pageSize,
        this.sortdir === 'asc' ? this.sort! : this.sort + '-',
        undefined,
        undefined,//campaignId
        undefined,//messagId
        undefined, //startdate
        undefined, //enddate
        this.channelTypeFilter,
        this.search || undefined
      )
      .pipe(map((result: MessageEventResultSet) => result as IResultSet<MessageEvent>));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
  public onFilterChanged(filterName: string, value: any) {
    if (filterName === 'messageChannel') {
      this.channelTypeSelectedOption = value;
      this.channelTypeFilter = [value];
      this.refresh();
    }

  }
}
