import { DataService } from './data.service';
import { Observable, of } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasePartialResultSet, CasesApiService, CaseTypePartialResultSet, FilterTerm } from './cases-api.service';
import { filter, map, find } from 'rxjs/operators';

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

    //TODO Pass code
    public getCaseType(code?: string): Observable<any> {
      const observable = this.getCaseTypeMenuItems().pipe(
        map(caseTypes => {
          return caseTypes.items?.find(x => x.code == "Pothen")
        })
      );

      return observable;
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
