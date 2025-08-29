import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CampaignStatistics, MessagesApiClient, CampaignMessageResponseResultSet, CampaignMessageResponse } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-campaign-messages',
  templateUrl: './campaign-messages.component.html'
})
export class CampaignMessagesComponent extends BaseListComponent<CampaignMessageResponse> implements OnInit {
  private _campaignId: string | undefined;
  public loaded = false;
  public counters: CampaignStatistics | undefined;

  constructor(
    route: ActivatedRoute,
    router: Router,
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _api: MessagesApiClient
  ) {
    super(route, router);
    this.view = ListViewType.Table;
    this.pageSize = 10;
    this.sort = 'createdOn';
    this.sortdir = 'asc';
    this.search = '';
    this.sortOptions = [
      new MenuOption('Ημ/νια Δημιουργίας', 'id'),
      new MenuOption('Τίτλος', 'title'),
      new MenuOption('Ενεργή Από', 'activePeriod.from')
    ];
  }
  public newItemLink: string | null = null;
  public full = true;

  public override ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
    super.ngOnInit();
  }

  public loadItems(): Observable<IResultSet<CampaignMessageResponse> | null | undefined> {
    return this._api
      .getCampaignMessages(this._campaignId!, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
      .pipe(map((result: CampaignMessageResponseResultSet) => (result as IResultSet<CampaignMessageResponse>)));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
}
