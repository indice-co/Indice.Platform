import { DataService } from './data.service';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, LookupItemResultSet } from './cases-api.service';
import { FilterClause } from '@indice/ng-components';

@Injectable({
    providedIn: 'root'
})
export class LookupsService extends DataService {
    private readonly lookups: string = 'lookups';

    constructor(
        private _api: CasesApiService) {
        super();
    }

    public getLookup(lookupName: string, filter_FilterTerms?: FilterClause[] | undefined): Observable<LookupItemResultSet> {
        return this.getDataFromCacheOrHttp(this.getCacheKey(lookupName, filter_FilterTerms), this._api.getLookup(lookupName,
                                                                                                                 undefined, undefined, undefined, undefined, // list option params
                                                                                                                 filter_FilterTerms?.map(x => x.toString())));
    }

    private getCacheKey(lookupName: string, filter_FilterTerms?: FilterClause[] | undefined): string {
        let cacheKey = `${this.lookups}.${lookupName}`;
        filter_FilterTerms?.forEach((f: FilterClause) => {
            cacheKey = cacheKey.concat(`.${f.member}.${f.value}`)
        });
        return cacheKey;
    }
}
