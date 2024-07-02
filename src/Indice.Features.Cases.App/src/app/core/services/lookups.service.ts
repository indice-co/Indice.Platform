import { DataService } from './data.service';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, FilterTerm, LookupItemResultSet } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class LookupsService extends DataService {
    private readonly lookups: string = 'lookups';

    constructor(
        private _api: CasesApiService) {
        super();
    }

    public getLookup(lookupName: string, filter_FilterTerms?: FilterTerm[] | undefined): Observable<LookupItemResultSet> {
        return this.getDataFromCacheOrHttp(this.getCacheKey(lookupName, filter_FilterTerms), this._api.getLookup(lookupName, filter_FilterTerms));
    }

    private getCacheKey(lookupName: string, filter_FilterTerms?: FilterTerm[] | undefined): string {
        let cacheKey = `${this.lookups}.${lookupName}`;
        filter_FilterTerms?.forEach((f: FilterTerm) => {
            cacheKey = cacheKey.concat(`.${f.key}.${f.value}`)
        });
        return cacheKey;
    }
}
