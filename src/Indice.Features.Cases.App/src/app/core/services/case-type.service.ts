import { DataService } from './data.service';
import { EMPTY, Observable, of } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, CaseTypePartial, CaseTypePartialResultSet, FilterTerm } from './cases-api.service';
import { map, switchMap } from 'rxjs/operators';

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

    public getCaseType(code: string): Observable<CaseTypePartial> {
      return this.getCaseTypeMenuItems().pipe(
        map(caseTypes => caseTypes.items?.find(x => x.code === code)),
        //using switchMap to filter out "undefined"
        switchMap(result => {
          if (result) {
            return of(result);
          } else {
            return EMPTY;
          }
        })
      );
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
