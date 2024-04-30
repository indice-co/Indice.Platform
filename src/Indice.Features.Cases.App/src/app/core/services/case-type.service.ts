import { DataService } from './data.service';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, CaseTypePartialResultSet, FilterTerm } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class CaseTypeService extends DataService {
    private readonly menuItems: string = 'menuItems';

    constructor(
        private _api: CasesApiService) {
        super();
    }

    public getCaseTypeMenuItems(filter_FilterTerms?: FilterTerm[] | undefined): Observable<CaseTypePartialResultSet> {
        return this.getDataFromCacheOrHttp(this.setCacheKey(filter_FilterTerms), this._api.getCaseTypes());
    }

    public getCaseType(code: string) {
      this.getCaseTypeMenuItems().subscribe(caseTypes => {
        return caseTypes.items?.find(x=> x.code == code);
      })
    }

    private setCacheKey(filter_FilterTerms?: FilterTerm[] | undefined): string {
        let cacheKey = this.menuItems;
        if (filter_FilterTerms) {
            filter_FilterTerms.forEach((f: FilterTerm) => {
                cacheKey = cacheKey.concat(`.${f.key}.${f.value}`);
            });
        }
        return cacheKey;
    }
}
