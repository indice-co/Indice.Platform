import { ActivatedRoute, Router } from '@angular/router';
import { Component, Input, OnInit } from '@angular/core';
import { DataService } from 'src/app/core/services/data.service';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { FilterClause } from '@indice/ng-components';
import { Location } from '@angular/common';

@Component({
    selector: 'app-risk-details-page',
    templateUrl: './risk-details-page.component.html',
    styleUrls: ['./risk-details-page.component.scss']
})

export class RiskDetailsPageComponent implements OnInit {
    @Input() object: any;

    constructor(private _api: RiskApiService, private activatedRoute: ActivatedRoute, private _router: Router, private _location: Location) {
    }

    ngOnInit(): void {
        const isRiskResult = this._location.path().includes('risk-results');
        if (isRiskResult) {
            const id = this.activatedRoute?.snapshot.paramMap.get('id');
            const idFilter = this.stringifyFilterClause({
                member: 'id',
                value: id,
                operator: 'eq',
                dataType: 'string',
            } as FilterClause);
            this._api.getRiskResults([idFilter], 1, 1).subscribe(result => {
                this.object = result.items[0];
            });
        } else {
            const id = this.activatedRoute?.snapshot.paramMap.get('id');
            const idFilter = this.stringifyFilterClause({
                member: 'id',
                value: id,
                operator: 'eq',
                dataType: 'string',
            } as FilterClause);
            this._api.getRiskEvents([idFilter], 1, 1).subscribe(event => {
                this.object = event.items[0];
            });
        }
    }

    private stringifyFilterClause(filter: FilterClause): string {
        return `${filter.member}::${filter.operator}::${filter.value.trim()}`;
    }

    closePanel() {
        const url = this._location.path().replace(/(rightpane:details.*)/, '');
        this._router.navigateByUrl(`${url}`);
    }
}

