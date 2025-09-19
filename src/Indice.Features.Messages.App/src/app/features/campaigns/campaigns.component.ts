import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, RouterViewAction, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';
import { Campaign, CampaignResultSet, MessagesApiClient, MessageType, MessageTypeResultSet } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-campaigns',
  templateUrl: './campaigns.component.html'
})
export class CampaignsComponent extends BaseListComponent<Campaign> implements OnInit {
  constructor(
    route: ActivatedRoute,
    router: Router,
    private _api: MessagesApiClient
  ) {
    super(route, router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'createdAt';
    this.sortdir = 'desc';
    this.search = '';
    this.sortOptions = [
      new MenuOption('Ημ/νια Δημιουργίας', 'createdAt'),
      new MenuOption('Τίτλος', 'title'),
      new MenuOption('Ενεργή Από', 'activePeriod.from'),
      new MenuOption('Τύπος', 'type.name'),
      new MenuOption('Δημοσιευμένη', 'published')
    ];
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
    super.ngOnInit();
    this.actions.push(new RouterViewAction(Icons.Add, 'campaigns/add-campaign', null, 'δημιουργία καμπάνιας'));
  }

  public loadItems(): Observable<IResultSet<Campaign> | null | undefined> {
    return this._api
      .getCampaigns(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, undefined, undefined, undefined, this.messageTypeFilter ? [this.messageTypeFilter] : undefined)
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
}
