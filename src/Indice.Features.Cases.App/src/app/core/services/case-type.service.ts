import { DataService } from './data.service';
import { forkJoin, Observable, of } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, CaseTypePartial, CaseTypePartialResultSet, CheckpointType } from './cases-api.service';
import { map, switchMap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class CaseTypeService extends DataService {
  private readonly key: string = 'caseTypeCacheKey';
  private readonly checkCreationKey: string = 'checkCreationKey';

  constructor(
    private _api: CasesApiService) {
    super();
  }

  public getCaseTypes(): Observable<CaseTypePartialResultSet> {
    return this.getDataFromCacheOrHttp(this.key, this._api.getCaseTypesList());
  }

  public invalidateCaseTypes(): void {
    this.removeDataFromCache(this.key);
    this.removeDataFromCache(this.checkCreationKey);
  }

  public getCaseType(code: string): Observable<CaseTypePartial | undefined> {
    return this.getCaseTypes().pipe(
      map(caseTypes => caseTypes.items?.find(x => x.code === code)),
    );
  }

  // Get case types, filter only menu items, cast to CaseTypeMenu
  public getCaseTypeMenuItems(): Observable<CaseTypeMenu[]> {
    return this.getCaseTypes().pipe(
      map(caseTypes =>
        (caseTypes.items?.filter(item => item.isMenuItem) || []).map(item => ({
          id: item.id,
          title: item.title,
          code: item.code,
          isMenuItem: item.isMenuItem ?? false,
          gridFilterConfig: item.gridFilterConfig,
          gridColumnConfig: item.gridColumnConfig
        }))
      )
    );
  }

  public getCanCreateCaseTypes(): Observable<CaseTypePartialResultSet> {
    return this.getDataFromCacheOrHttp(this.checkCreationKey, this._api.getCaseTypesList(true));
  }

  public getCheckpointTypes(distinct = false): Observable<CheckpointType[]> {
    if (distinct) {
      return this._api.getCheckpointTypesList(undefined, undefined, undefined, undefined, undefined, true).pipe(
        map(result => result.items ?? [])
      );
    }
    const pageSize = 1000;
    return this._api.getCheckpointTypesList(1, pageSize, 'caseTypeCode').pipe(
      switchMap(result => {
        const items = result.items ?? [];
        const pageCount = Math.ceil((result.count ?? items.length) / pageSize);
        if (pageCount <= 1) {
          return of(items);
        }
        return forkJoin(Array.from(
          { length: pageCount - 1 },
          (_, index) => this._api.getCheckpointTypesList(index + 2, pageSize, 'caseTypeCode')
        )).pipe(
          map(pages => [...items, ...pages.flatMap(page => page.items ?? [])])
        );
      })
    );
  }
}

class CaseTypeMenu {
  id: string | undefined = "";
  title: string | undefined = "";
  code: string | undefined = "";
  isMenuItem: boolean = false;
  gridFilterConfig: string | undefined = "";
  gridColumnConfig: string | undefined = "";
}
