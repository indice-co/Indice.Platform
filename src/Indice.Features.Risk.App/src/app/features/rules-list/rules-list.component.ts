import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { BaseListComponent, IResultSet, Icons, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { User } from 'oidc-client-ts';
import { Observable, Subscription } from 'rxjs';
import { map, take, tap } from 'rxjs/operators';
import { DataService } from 'src/app/core/services/data.service';
import { ParamsService } from 'src/app/core/services/params.service';
import { RiskApiService, RiskRuleDto, RiskRuleDtoResultSet } from 'src/app/core/services/risk-api.service';

@Component({
    selector: 'app-rules-list',
    templateUrl: './rules-list.component.html'
})
export class RulesListComponent extends BaseListComponent<RiskRuleDto> implements OnInit {
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
        private dataService: DataService
    ) { 
        super(_route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 50;
        this.sort = 'enabled';
        this.sortdir = 'desc';
        this.sortOptions = [
            new MenuOption('Active', 'enabled')
        ];
    }

    public ngOnInit(): void {
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

    async openRuleOptionsPane(ruleName: string) {
        const options: { [key: string]: string } = await this.loadRuleOptions(ruleName);
        this.dataService.setInputData(options);
        this._router.navigateByUrl('/rules(rightpane:options)', {skipLocationChange: true});
    }

    async loadRuleOptions(ruleName: string): Promise<{ [key: string]: string }> {
        return new Promise<{ [key: string]: string }>((resolve, reject) => {
          this._api
            .getRiskRuleOptions(ruleName)
            .subscribe({
            next: (result) => {
              resolve({ ...result });
            },
            error: (error) => {
              reject(error);
            }
          });
        });
      }

    loadItems(): Observable<IResultSet<RiskRuleDtoResultSet>> {
        this._paramsService.setParams({
            view: this.view,
            page: this.page,
            pagesize: this.pageSize,
            search: this.search,
            sort: this.sort,
            dir: this.sortdir,
        });

        return this._api
            .getRiskRules()
            .pipe(
                take(1),
                map((result: RiskRuleDtoResultSet) => (result as IResultSet<RiskRuleDto>))
            );
    }
}
