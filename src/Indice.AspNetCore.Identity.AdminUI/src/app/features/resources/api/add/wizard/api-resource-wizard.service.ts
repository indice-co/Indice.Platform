import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { IdentityApiService, ClaimTypeInfo, ClaimTypeInfoResultSet } from 'src/app/core/services/identity-api.service';

@Injectable()
export class ApiResourceWizardService {
    private _allClaims: AsyncSubject<ClaimTypeInfo[]>;

    constructor(private _api: IdentityApiService) { }

    public getAllClaims(): Observable<ClaimTypeInfo[]> {
        if (!this._allClaims) {
            this._allClaims = new AsyncSubject<ClaimTypeInfo[]>();
            this._api.getClaimTypes(undefined, 1, 2147483647, 'name+', undefined).subscribe((response: ClaimTypeInfoResultSet) => {
                this._allClaims.next(response.items);
                this._allClaims.complete();
            });
        }
        return this._allClaims;
    }
}
