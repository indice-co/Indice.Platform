import { DataService } from './data.service';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, CaseTypePartial, CaseTypePartialResultSet, CheckpointType } from './cases-api.service';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class CaseTypeService extends DataService {
  private readonly key: string = 'caseTypeCacheKey';
  private readonly checkCreationKey: string = 'checkCreationKey';
  private readonly distinctCheckpointsKey: string = 'distinctCheckpointsKey';

  constructor(
    private _api: CasesApiService) {
    super();
  }

  public getCaseTypes(): Observable<CaseTypePartialResultSet> {
    return this.getDataFromCacheOrHttp(this.key, this._api.getCaseTypesList());
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

  public getDistinctCheckpointTypes():Observable<CheckpointType[]> {
    return this.getDataFromCacheOrHttp(this.distinctCheckpointsKey, this._api.getDistinctCheckpointTypes());
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
