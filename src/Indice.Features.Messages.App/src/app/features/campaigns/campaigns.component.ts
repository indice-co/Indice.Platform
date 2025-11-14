import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { APP_LANGUAGES, BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, RouterViewAction, ViewAction } from '@indice/ng-components';
import { Observable, combineLatest, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Campaign, CampaignResultSet, MessagesApiClient, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaigns.component.html',
    standalone: false
})
export class CampaignsComponent extends BaseListComponent<Campaign> implements OnInit, OnDestroy {
  private readonly _destroy$ = new Subject<void>();

  constructor(
    route: ActivatedRoute,
    router: Router,
    private _api: MessagesApiClient,
    @Inject(APP_LANGUAGES) private _lang: AppLanguagesService
  ) {
    super(route, router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'createdAt';
    this.sortdir = 'desc';
    this.search = '';
    // Will populate after translations load.
    this.sortOptions = [];
  }

  public messageTypeFilter: any;

  public newItemLink: string | null = null;
  public full = true;
  public messageTypeOptions$ = this._api
    .getMessageTypes(1, 100, undefined, undefined)
    .pipe(
      map((result: MessageTypeResultSet) => (result.items || []).map(x => new MenuOption(x.name!, x.id, undefined, x, `dot dot-${x.classification} mr-2`))),
      map(options => options ?? undefined)
    );

  public override ngOnInit(): void {
    this._initTranslations();
    super.ngOnInit();
    this.actions.push(new RouterViewAction(Icons.Add, 'campaigns/add-campaign', null, 'Campaigns.CreateCampaignAction'));
    this._lang.translateKey('Campaigns.CreateCampaignAction').pipe(takeUntil(this._destroy$)).subscribe(actionName => {
      var addAction = this.actions.filter(o => o.icon == Icons.Add)
      addAction[0]!.tooltip = actionName
    })
  }

  private _initTranslations(): void {
    const createdOn$ = this._lang.translateKey('Campaigns.SortCreatedOnOption');
    const title$ = this._lang.translateKey('Campaigns.SortTitleOption');
    const activeFrom$ = this._lang.translateKey('Campaigns.SortActiveFromOption');
    const type$ = this._lang.translateKey('Campaigns.SortTypeOption');
    const published$ = this._lang.translateKey('Campaigns.SortPublishedOption');
    //const createAction$ = this._lang.translateKey('Campaigns.CreateCampaignAction');

    combineLatest([createdOn$, title$, activeFrom$, type$, published$])
      .pipe(takeUntil(this._destroy$))
      .subscribe(([createdOn, title, activeFrom, type, published,]) => {
        this.sortOptions = [
          new MenuOption(createdOn || 'Campaigns.SortCreatedOnOption', 'createdAt'),
          new MenuOption(title || 'Campaigns.SortTitleOption', 'title'),
          // field activePeriod.from kept as originally
          new MenuOption(activeFrom || 'Campaigns.SortActiveFromOption', 'activePeriod.from'),
          new MenuOption(type || 'Campaigns.SortTypeOption', 'type.name'),
          new MenuOption(published || 'Campaigns.SortPublishedOption', 'published')
        ];
      });

  }

  public loadItems(): Observable<IResultSet<Campaign> | null | undefined> {
    return this._api
      .getCampaigns(
        this.page,
        this.pageSize,
        this.sortdir === 'asc' ? this.sort! : this.sort + '-',
        this.search || undefined,
        undefined,
        undefined,
        undefined,
        this.messageTypeFilter ? [this.messageTypeFilter] : undefined
      )
      .pipe(map((result: CampaignResultSet) => (result as IResultSet<Campaign>)));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }

  public onFilterChanged(filterName: string, value: any) {
    if (filterName === 'messageType') {
      this.messageTypeFilter = value;
      this.refresh();
    }
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }
}
