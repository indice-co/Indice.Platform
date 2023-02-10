import { GroupByReportResult, ReportTag } from './../../../core/services/cases-api.service';
import { Component, Input, OnInit } from '@angular/core';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { ChartItem, ChartType } from 'chart.js';
import Chart from 'chart.js/auto'
import { Router } from '@angular/router';

@Component({
  selector: 'app-canvas-tile',
  templateUrl: './canvas-tile.component.html',
  styleUrls: ['./canvas-tile.component.scss']
})
export class CanvasTileComponent implements OnInit {
  @Input() canvasId: ReportTag | undefined;
  @Input() title: string | undefined;

  public loading = false;
  public sum: number | undefined;
  public backgroundColors: string[] = ['rgb(22, 77, 85)', 'rgb(27, 135, 116)', 'rgb(64, 151, 162)', 'rgb(181, 206, 209)', 'rgb(3, 41, 46)'];

  constructor(
    private router: Router,
    private _api: CasesApiService) { }

  ngOnInit(): void {
    this.loading = true;
    this._api.getCaseReport(this.canvasId).subscribe(
      (results: GroupByReportResult[]) => {
        this.createChart(results);
        this.loading = false;
      }
    );
  }

  public navigate(path: string): void {
    this.router.navigateByUrl(path);
  }

  private createChart(results: GroupByReportResult[]) {
    var ctx = document.getElementById(this.canvasId!) as ChartItem;
    const labels: string[] = results.map(x => x.label!.toString());
    const counts: number[] = results.map(x => x.count!);
    this.sum = counts.reduce((a, b) => a + b);
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
          legend: {
            display: false
          },
          title: {
            display: false
          }
        }
      },
    };
    // create chart
    new Chart(ctx, config);
  }
}
