import { DataService } from './data.service';
import { Observable } from 'rxjs';
import { Injectable } from '@angular/core';
import { CasesApiService, CaseTypePartial, CaseTypePartialResultSet } from './cases-api.service';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class CaseTypeService extends DataService {
  private readonly key: string = 'caseTypeCacheKey';

  constructor(
    private _api: CasesApiService) {
    super();
  }

  public getCaseTypes(): Observable<CaseTypePartialResultSet> {
    return this.getDataFromCacheOrHttp(this.key, this._api.getCaseTypes());
  }

  public getCaseType(code: string): Observable<CaseTypePartial | undefined> {
    return this.getCaseTypes().pipe(
      map(caseTypes => caseTypes.items?.find(x => x.code === code)),
    );
  }

  public getCaseTypeMenuItems(): Observable<CaseTypeMenu[]> {
    // Get case types, filter only menu items, cast to CaseTypeMenu
    return this.getCaseTypes().pipe(
      map(caseTypes => {
        const menuItems = caseTypes.items?.filter(item => item.isMenuItem) || [];
        return menuItems.map(item => {
          const menu = new CaseTypeMenu();
          menu.id = item.id;
          menu.title = item.title;
          menu.code = item.code;
          menu.isMenuItem = item.isMenuItem ?? false;
          menu.gridFilterConfig = item.gridFilterConfig
          menu.gridColumnConfig = item.gridColumnConfig
          return menu;
        });
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
