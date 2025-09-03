import { ActivatedRoute } from '@angular/router';
import { Component, Input, OnInit } from '@angular/core';
import { DataService } from 'src/app/core/services/data.service';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { FilterClause } from '@indice/ng-components';

@Component({
    selector: 'app-risk-details-page',
    templateUrl: './risk-details-page.component.html',
    styleUrls: ['./risk-details-page.component.scss']
})

export class RiskDetailsPageComponent implements OnInit {
    @Input() extraData: string;
    
    constructor(private _api: RiskApiService, private activatedRoute: ActivatedRoute) {
    }

    ngOnInit(): void {
        const id = this.activatedRoute?.snapshot.paramMap.get('id');
        const idFilter = this.stringifyFilterClause({
            member: 'id',
            value: id,
            operator: 'eq',
            dataType: 'string',
        } as FilterClause);
        this._api.getRiskEvents([idFilter], 1, 1).subscribe(event => {
            this.extraData = event.items[0].data;
        });
    }

        private stringifyFilterClause(filter: FilterClause): string {
        return `${filter.member}::${filter.operator}::${filter.value.trim()}`;
    }
}

