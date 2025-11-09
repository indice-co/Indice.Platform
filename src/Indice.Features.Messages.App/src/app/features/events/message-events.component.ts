import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable, Subject, combineLatest } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { MessagesApiClient, MessageEvent, MessageEventResultSet, MessageChannelKind } from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';

@Component({
  selector: 'app-message-events',
  templateUrl: './message-events.component.html'
})
export class MessageEventsComponent extends BaseListComponent<MessageEvent> implements OnInit, OnDestroy {
  constructor(
    route: ActivatedRoute,
    private _router: Router,
    private _api: MessagesApiClient,
    private _languages: AppLanguagesService
  ) {
    super(route, _router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'createdOn';
    this.sortdir = 'desc';
    this.search = '';
    // Fallback: use translation keys until translated values arrive.
    this.sortOptions = [
      new MenuOption('Events.SortCreatedOnOption', 'createdOn'),
      new MenuOption('Events.SortRecipientOption', 'recipient')
    ];
  }

  private _destroy$ = new Subject<void>();

  public channelTypeFilter: MessageChannelKind[] | undefined = undefined;
  public channelTypeSelectedOption: MessageChannelKind[] | undefined = undefined;
  public newItemLink: string | null = null;

  public override ngOnInit(): void {
    super.ngOnInit();
    // Observe & update sort option labels reactively.
    const sortKeys = this.sortOptions.map(o => o.text);
    combineLatest(sortKeys.map(k => this._languages.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
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
