import { AfterViewInit, Component, Input } from '@angular/core';
import { Chart } from 'chart.js/auto';
import { CommonModule, DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-doughnut-chart',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './doughnut-chart.component.html',
  styleUrl: './doughnut-chart.component.css'
})

export class DoughnutChartComponent implements AfterViewInit {
  public fixedDuration = 0;
  public variableDuration = 0;
  public chart: any;

  @Input() stringValue: string = '';
  @Input() title: string | undefined;
  @Input() type: MarketabilityChartType = MarketabilityChartType.Hottness;
  Color: any;
  value: number = 0;

  constructor() { }

  ngAfterViewInit(): void {
    try {
      this.value = Number(this.stringValue);
    }
    catch {
      console.log("chart value not a number")
      this.value = 0;
    }
    if (!this.chart) {
      this.createChart();
    } else {
      this.chart.data.datasets[0].data = [this.value, 5 - this.value];
      this.chart.update();
    }
  }

  ngOnChanges() {
    if (!this.chart) {
      this.createChart();
    } else {
      this.chart.data.datasets[0].data = [this.value, 5 - this.value];
      this.chart.update();
    }
  }


  private createChart() {
    this.Color = this.type == MarketabilityChartType.Hottness ? "#0BC582" : "#0BC1C0";
    if (this.value) {
      this.chart = new Chart(this.title ?? '', {
        type: 'doughnut',
        data: {
          datasets: [{
            data: [this.value, 5 - this.value],
            backgroundColor: [
              this.Color,
              '#DDE4E8'
            ],
            hoverOffset: 4,
            borderColor: [
              this.Color,
              '#DDE4E8'
            ],
          }]
        },
        options: {
          cutout: '80%',
          radius: '100%',
        }
      });
      console.log(this.title, this.chart)
    }
  }
}

export enum MarketabilityChartType {
  Liquidity = 0,
  Hottness = 1
}
