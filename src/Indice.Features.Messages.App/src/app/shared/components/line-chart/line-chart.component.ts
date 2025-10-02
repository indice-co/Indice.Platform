import { Component, Input, OnInit, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import Chart from 'chart.js/auto';

export interface LineChartDataset {
  label: string;
  data: number[];
  borderColor?: string;
  backgroundColor?: string;
}

export interface LineChartData {
  labels: (string|Date|number)[];
  datasets: LineChartDataset[];
}

@Component({
    selector: 'app-line-chart',
    template: `
    <div class="chart-container">
      <canvas #chartCanvas></canvas>
      <div class="no-data-message" *ngIf="!hasData">
        No data available
      </div>
    </div>
  `,
    styles: [`
    .chart-container {
      position: relative;
      height: 100%;
      width: 100%;
    }
    
    canvas {
      width: 100% !important;
      height: 100% !important;
    }
    
    .no-data-message {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      justify-content: center;
      align-items: center;
      font-size: 14px;
      color: #999;
      background-color: rgba(255, 255, 255, 0.8);
    }
  `],
    standalone: false
})
export class LineChartComponent implements OnInit, AfterViewInit {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;
  
  @Input() data: LineChartData = {
    labels: [],
    datasets: []
  };
  
  @Input() options: any = {};
  
  private chart: Chart | null = null;
  
  get hasData(): boolean {
    return this.data && 
           this.data.datasets && 
           this.data.datasets.length > 0 && 
           this.data.datasets.some(dataset => dataset.data && dataset.data.length > 0);
  }

  constructor() { }

  ngOnInit(): void {
    // Set default options if none provided
    this.options = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'top',
        },
        tooltip: {
          mode: 'index',
          intersect: false,
        },
      },
      scales: {
        y: {
          beginAtZero: true,
        },
      },
      ...this.options
    };
  }
  
  ngAfterViewInit(): void {
    this.createChart();
  }
  
  ngOnChanges(): void {
    this.updateChart();
  }
  
  private createChart(): void {
    if (!this.chartCanvas || !this.hasData) {
      return;
    }
    
    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    if (ctx) {
      this.chart = new Chart(ctx, {
        type: 'line',
        data: this.data,
        options: this.options
      });
    }
  }
  
  private updateChart(): void {
    if (this.chart) {
      this.chart.data = this.data;
      this.chart.options = this.options;
      this.chart.update();
    } else {
      this.createChart();
    }
  }
  
  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
  }
}
