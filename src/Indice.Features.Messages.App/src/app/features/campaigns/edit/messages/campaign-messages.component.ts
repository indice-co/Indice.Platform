import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable, combineLatest, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { RecipientMetrics, MessagesApiClient, Recipient, RecipientResultSet } from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';

@Component({
    selector: 'app-campaign-messages',
    templateUrl: './campaign-messages.component.html',
    standalone: false
})
export class CampaignMessagesComponent extends BaseListComponent<Recipient> implements OnInit, OnDestroy {
  public _campaignId: string | undefined;
  public loaded = false;
  public counters: RecipientMetrics | undefined;

  private readonly _destroy$ = new Subject<void>();

  constructor(
    route: ActivatedRoute,
    router: Router,
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _api: MessagesApiClient,
    private readonly _lang: AppLanguagesService
  ) {
    super(route, router);
    this.view = ListViewType.Table;
    this.pageSize = 10;
    this.sort = 'createdOn';
    this.sortdir = 'asc';
    this.search = '';
    // sortOptions will be set after translations load
    this.sortOptions = [];
  }

  public newItemLink: string | null = null;
  public full = true;

  public override ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
    this._initSortTranslations();
    super.ngOnInit();
  }

  private _initSortTranslations(): void {
    const createdOn$ = this._lang.translateKey('Campaigns.SortCreatedOnOption');
    const title$ = this._lang.translateKey('Campaigns.SortTitleOption');
    const activeFrom$ = this._lang.translateKey('Campaigns.SortActiveFromOption');
    combineLatest([createdOn$, title$, activeFrom$])
      .pipe(takeUntil(this._destroy$))
      .subscribe(([createdOn, title, activeFrom]) => {
        this.sortOptions = [
          new MenuOption(createdOn || 'Campaigns.SortCreatedOnOption', 'id'),
          new MenuOption(title || 'Campaigns.SortTitleOption', 'title'),
          new MenuOption(activeFrom || 'Campaigns.SortActiveFromOption', 'activePeriod.from')
        ];
      });
  }

  public loadItems(): Observable<IResultSet<Recipient> | null | undefined> {
    return this._api
      .getCampaignMessages(this._campaignId!, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
      .pipe(map((result: RecipientResultSet) => (result as IResultSet<Recipient>)));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }

  public CheckReceivePreference(communicationPreferences: string[], option: string): boolean {
    return communicationPreferences.findIndex(obj => obj === option) >= 0;
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
    super.ngOnDestroy();
    
  }
}
