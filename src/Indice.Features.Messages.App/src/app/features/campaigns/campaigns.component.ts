import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, RouterViewAction, ViewAction } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Campaign, CampaignResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaigns.component.html'
})
export class CampaignsComponent extends BaseListComponent<Campaign> implements OnInit {
    constructor(
        route: ActivatedRoute,
        private _translate: TranslateService,
        router: Router,
        private _api: MessagesApiClient
    ) {
        super(route, router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'createdAt';
        this.sortdir = 'desc';
        this.search = '';
        this.sortOptions = [
            new MenuOption(this._translate.instant('campaigns.created-at'), 'createdAt'),
            new MenuOption(this._translate.instant('campaigns.title'), 'title'),
            new MenuOption(this._translate.instant('campaigns.active-from'), 'activePeriod.from')
        ];
    }

    public newItemLink: string | null = null;
    public full = true;

    public override ngOnInit(): void {
        super.ngOnInit();
        this.actions.push(new RouterViewAction(Icons.Add, 'campaigns/add-campaign', null, null));
    }

    public loadItems(): Observable<IResultSet<Campaign> | null | undefined> {
        return this._api
            .getCampaigns(undefined, undefined, this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: CampaignResultSet) => (result as IResultSet<Campaign>)));
    }

    public override actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
