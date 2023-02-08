import { Component, Inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { HeaderMetaItem } from '@indice/ng-components';
import { User } from 'oidc-client';
import { of, Subscription } from 'rxjs';
import { take, catchError } from 'rxjs/operators';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { ChartType, ChartItem } from 'chart.js';
import Chart from 'chart.js/auto'

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    public metaItems: HeaderMetaItem[] | null = [];
    public loaded = false;
    public cases: any;
    public user: User | null = null;
    public userSub$: Subscription | null = null;
    public backgroundColors: string[] = ['rgb(22, 77, 85)', 'rgb(27, 135, 116)', 'rgb(64, 151, 162)', 'rgb(181, 206, 209)', 'rgb(3, 41, 46)'];

    constructor(
        private router: Router,
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
        const cases$ = this._api
            .getCases(
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                undefined,
                1,
                0,
                undefined,
                undefined,
                undefined
            )
            .pipe(
                take(1),
                catchError((err) => {
                    return of({ error: err, isError: true });
                }));

        this._api.getCasesGroupedByStatus().subscribe(
            (result) => {
                var ctx = document.getElementById('grouped-by-status') as ChartItem;
                const labels: string[] = result.map(x => x.label!.toString());
                const counts: number[] = result.map(x => x.count!);
                var chartType: ChartType = 'doughnut'

                const config = {
                    type: chartType,
                    data: {
                        labels: labels,
                        datasets: [
                            {
                                label: 'Πλήθος αιτήσεων',
                                data: counts,
                                backgroundColor: this.backgroundColors,
                            }
                        ]
                    },
                    options: {
                        responsive: true,
                        plugins: {
                            title: {
                                display: true,
                                text: 'Αιτήσεις ανά Τρέχον Σημείο Ελέγχου'
                            }
                        }
                    },
                };
                // create chart
                new Chart(ctx, config);
            }
        );

        this._api.getCasesGroupedByCaseType().subscribe(
            (result) => {
                var ctx = document.getElementById('grouped-by-casetype') as ChartItem;
                const labels: string[] = result.map(x => x.label!.toString());
                const counts: number[] = result.map(x => x.count!);
                var chartType: ChartType = 'bar'

                const config = {
                    type: chartType,
                    data: {
                        labels: labels,
                        datasets: [
                            {
                                label: 'Πλήθος αιτήσεων',
                                data: counts,
                                backgroundColor: this.backgroundColors,
                            }
                        ]
                    },
                    options: {
                        responsive: true,
                        plugins: {
                            legend: {
                                display: false
                            },
                            title: {
                                display: true,
                                text: 'Αιτήσεις ανά Τύπο Αίτησης'
                            }
                        },
                        scales: {
                            x: {
                                stacked: true,
                            },
                            y: {
                                stacked: true
                            }
                        }
                    },
                };
                // create chart
                new Chart(ctx, config);
            }
        );

        this._api.getCasesGroupedByGroupId().subscribe(
            (result) => {
                var ctx = document.getElementById('grouped-by-groupid') as ChartItem;
                const labels: string[] = result.map(x => `Κατάστημα ${x.label!.toString()}`);
                const counts: number[] = result.map(x => x.count!);
                var chartType: ChartType = 'bar'

                const config = {
                    type: chartType,
                    data: {
                        labels: labels,
                        datasets: [
                            {
                                label: 'Πλήθος αιτήσεων',
                                data: counts,
                                backgroundColor: this.backgroundColors,
                            }
                        ]
                    },
                    options: {
                        responsive: true,
                        plugins: {
                            legend: {
                                display: false
                            },
                            title: {
                                display: true,
                                text: 'Αιτήσεις ανά Κατάστημα'
                            }
                        },
                        scales: {
                            x: {
                                stacked: true,
                            },
                            y: {
                                stacked: true
                            }
                        }
                    },
                };
                // create chart
                new Chart(ctx, config);
            }
        );

        cases$.subscribe(results => {
            this.cases = results;
            this.loaded = true;
        });
    }

    public navigate(path: string): void {
        this.router.navigateByUrl(path);
    }
}
