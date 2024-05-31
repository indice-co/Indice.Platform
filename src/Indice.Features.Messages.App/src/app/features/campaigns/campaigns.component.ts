import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, RouterViewAction, ViewAction } from '@indice/ng-components';
import { Campaign, CampaignResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaigns.component.html'
})
export class CampaignsComponent extends BaseListComponent<Campaign> implements OnInit, OnDestroy {
    private langChangeSubscription: Subscription | null = null;

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
    }

    public newItemLink: string | null = null;
    public full = true;

    public override ngOnInit(): void {
        super.ngOnInit();
        this.actions.push(new RouterViewAction(Icons.Add, 'campaigns/add-campaign', null, null));

        this.langChangeSubscription = this._translate.onLangChange.subscribe(() => {
            this.updateMenuOptions(); 
        });
        this.updateMenuOptions(); 
    }

    public override ngOnDestroy(): void {
        if (this.langChangeSubscription) {
            this.langChangeSubscription.unsubscribe();
        }
    }

    private updateMenuOptions(): void {
        this.sortOptions = [
            new MenuOption(this._translate.instant('campaigns.created-at'), 'createdAt'),
            new MenuOption(this._translate.instant('campaigns.title'), 'title'),
            new MenuOption(this._translate.instant('campaigns.active-from'), 'activePeriod.from')
        ];
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
