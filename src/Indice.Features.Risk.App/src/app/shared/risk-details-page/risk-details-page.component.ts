import { Component, Input, OnInit } from '@angular/core';
import { DataService } from 'src/app/core/services/data.service';

@Component({
    selector: 'app-risk-details-page',
    templateUrl: './risk-details-page.component.html',
    styleUrls: ['./risk-details-page.component.scss']
})

export class RiskDetailsPageComponent implements OnInit {
    @Input() extraData: string;
    
    constructor(private dataService: DataService) {

    }

    ngOnInit(): void {
        this.dataService.inputData$.subscribe(data => {
            this.extraData = data;
        });
    }
}

