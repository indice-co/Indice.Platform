import { Chart } from 'chart.js/auto';
import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { HeaderMetaItem } from '@indice/ng-components';
import { ChartItem, ChartType } from 'chart.js';
import { User } from 'oidc-client';
import { Subscription } from 'rxjs';
import { CasesApiService, ReportTag } from 'src/app/core/services/cases-api.service';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    public metaItems: HeaderMetaItem[] | null = [];
    public user: User | null = null;
    public userSub$: Subscription | null = null;
    public backgroundColors: string[] = ['rgb(22, 77, 85)', 'rgb(27, 135, 116)', 'rgb(64, 151, 162)', 'rgb(181, 206, 209)', 'rgb(3, 41, 46)'];
    reportTag = ReportTag;
    constructor(
        private _api: CasesApiService,
        @Inject(AuthService) private authService: AuthService
    ) { }

    ngOnInit(): void {
        this.authService.loadUser().subscribe((user) => {
            this.user = user;
        }, error => {
            console.error(error);
        });
        // Detect user changes and display / or not user info accordingly...
        this.userSub$ = this.authService.user$.subscribe((user: any) => {
            this.user = user;
        });
        this.metaItems = [];

        // this._api.getCasesGroupedByGroupId().subscribe(
        //     (result) => {
        //         var ctx = document.getElementById('grouped-by-groupid') as ChartItem;
        //         const labels: string[] = result.map(x => `Κατάστημα ${x.label!.toString()}`);
        //         const counts: number[] = result.map(x => x.count!);
        //         var chartType: ChartType = 'bar'

        //         const config = {
        //             type: chartType,
        //             data: {
        //                 labels: labels,
        //                 datasets: [
        //                     {
        //                         label: 'Πλήθος αιτήσεων',
        //                         data: counts,
        //                         backgroundColor: this.backgroundColors,
        //                     }
        //                 ]
        //             },
        //             options: {
        //                 responsive: true,
        //                 plugins: {
        //                     legend: {
        //                         display: false
        //                     },
        //                     title: {
        //                         display: true,
        //                         text: 'Αιτήσεις ανά Κατάστημα'
        //                     }
        //                 },
        //                 scales: {
        //                     x: {
        //                         stacked: true,
        //                     },
        //                     y: {
        //                         stacked: true
        //                     }
        //                 }
        //             },
        //         };
        //         // create chart
        //         new Chart(ctx, config);
        //     }
        // );

    }

}
