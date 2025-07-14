import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Campaign, CampaignResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-contact-campaigns',
  templateUrl: './contact-campaigns.component.html'
})
export class ContactCampaignsComponent extends BaseListComponent<Campaign> implements OnInit {
  

  constructor(
    route: ActivatedRoute,
    router: Router,
    private _activatedRoute: ActivatedRoute,
    private _api: MessagesApiClient
  ) {
    super(route, router);
    this.view = ListViewType.Table;
    this.pageSize = 10;
    this.sort = 'createdAt';
    this.sortdir = 'asc';
    this.search = '';
    this.sortOptions = [
      new MenuOption('Ημ/νια Δημιουργίας', 'createdAt'),
      new MenuOption('Τίτλος', 'title'),
      new MenuOption('Ενεργή Από', 'activePeriod.from')
    ];
  }
  private _contactId: string = '';
  public newItemLink: string | null = null;
  public full = true;

  public override ngOnInit(): void {
    this._contactId = this._activatedRoute.parent?.snapshot.params['contactId'];
    super.ngOnInit();
  }

  public loadItems(): Observable<IResultSet<Campaign> | null | undefined> {
    return this._api
      .getCampaigns(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, undefined, undefined, this._contactId)
      .pipe(map((result: CampaignResultSet) => (result as IResultSet<Campaign>)));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
}
