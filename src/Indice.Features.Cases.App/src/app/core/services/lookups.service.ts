import { DataService } from './data.service';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, LookupItemResultSet } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class LookupsService extends DataService {
    private readonly lookups: string = 'lookups';

    constructor(
        private _api: CasesApiService) {
        super();
    }

    public getLookup(lookupName: string): Observable<LookupItemResultSet> {
        return this.getDataFromCacheOrHttp(`${this.lookups}.${lookupName}`, this._api.getLookup(lookupName));
    }

}
