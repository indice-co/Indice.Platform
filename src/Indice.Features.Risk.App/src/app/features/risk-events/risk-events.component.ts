import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, RouterViewAction, ViewAction } from '@indice/ng-components';
import { FilterClause, SearchOption } from '@indice/ng-components/lib/controls/advanced-search/models';
import { User } from 'oidc-client-ts';
import { Observable, Subscription } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { ParamsService } from 'src/app/core/services/params.service';
import { RiskApiService, RiskEvent, RiskEventResultSet } from 'src/app/core/services/risk-api.service';
import { DataService } from 'src/app/core/services/data.service';

@Component({
    selector: 'app-risk-events',
    templateUrl: './risk-events.component.html'
})
export class RiskEventsComponent extends BaseListComponent<RiskEvent> implements OnInit {
    newItemLink: string;
    public formActions: ViewAction[] = [
        new ViewAction('refresh', 'refresh', null, Icons.Refresh, 'Ανανέωση στοιχείων')
    ];
    public user: User | null = null;
    public isAdmin: boolean | undefined;
    public userSub$: Subscription | null = null;

    constructor(
        @Inject(AuthService) private authService: AuthService,
        private _route: ActivatedRoute,
        private _router: Router,
        private _api: RiskApiService,
        private _paramsService: ParamsService,
        private dataService: DataService) 
    {
        super(_route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 50;
        this.sort = 'createdAt';
        this.sortdir = 'desc';
        this.sortOptions = [
            new MenuOption('Ημ. δημιουργίας', 'createdAt')
        ];

        const extraSearchOptions: SearchOption[] = [
            {
                field: 'subjectId',
                name: 'ΚΩΔΙΚΟΣ ΠΕΛΑΤΗ',
                dataType: 'string'
            },
            {
                field: 'ipAddress',
                name: 'IP ADDRESS',
                dataType: 'string'
            },
            {
                field: 'name',
                name: 'ΟΝΟΜΑ ΡΙΣΚΟΥ',
                dataType: 'string'
            },
            {
                field: 'type',
                name: 'ΤΥΠΟΣ ΡΙΣΚΟΥ',
                dataType: 'string'
            },
            {
                field: 'daterange',
                name: 'ΗΜ. ΔΗΜΙΟΥΡΓΙΑΣ',
                dataType: 'daterange'
            },
            {
                field: 'id',
                name: 'ΚΩΔΙΚΟΣ ΣΥΜΒΑΝΤΟΣ',
                dataType: 'string'
            }
        ];
        this.searchOptions.push(...extraSearchOptions);
    }

    openRiskDetailsPane(extraData: any): void {
        const dataJson = JSON.parse(JSON.stringify(extraData));
        this.dataService.setInputData(dataJson);
        this._router.navigateByUrl('/risk-events(rightpane:details)', {skipLocationChange: true});
    }

    ngOnInit(): void {
        this.authService.loadUser().subscribe((user) => {
            this.user = user;
            this.isAdmin = this.authService.isAdmin();
        }, error => {
            console.error(error);
        });
        this.userSub$ = this.authService.user$.subscribe((user: any) => {
            this.user = user;
            this.isAdmin = this.authService.isAdmin();
        });

        super.ngOnInit();
    }

    loadItems(): Observable<IResultSet<RiskEvent>> {
        let extraFilters: string[] = []

        this.filters?.forEach(x => extraFilters.push(this.stringifyFilterClause(x)));

        this._paramsService.setParams({
            view: this.view,
            page: this.page,
            pagesize: this.pageSize,
            search: this.search,
            sort: this.sort,
            dir: this.sortdir,
        });

        return this._api
            .getRiskEvents(
                extraFilters,
                this.page,
                this.pageSize,
                this.sortdir === 'asc' ? this.sort! : this.sort + '-',
                this.search || undefined
          
            )
            .pipe(
                take(1),
                map((result: RiskEventResultSet) => (result as IResultSet<RiskEvent>))
            );
    }

    private stringifyFilterClause(filter: FilterClause): string {
        return `${filter.member}::${filter.operator}::${filter.value.trim()}`;
    }
}
