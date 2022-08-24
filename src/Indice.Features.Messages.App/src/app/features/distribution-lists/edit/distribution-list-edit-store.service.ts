import { Injectable } from '@angular/core';

import { AsyncSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { DistributionList, MessagesApiClient, Template, UpdateDistributionListRequest, UpdateTemplateRequest } from 'src/app/core/services/messages-api.service';

@Injectable({
    providedIn: 'root'
})
export class DistributionListEditStore {
    private _distributionList: AsyncSubject<DistributionList> | undefined;
    private _idChanged = false;
    private _currentId = '';

    constructor(
        private _api: MessagesApiClient
    ) { }

    public getDistributionList(distributionListId: string): Observable<DistributionList> {
        this._idChanged = this._currentId !== distributionListId;
        this._currentId = distributionListId;
        if (!this._distributionList || this._idChanged) {
            this._distributionList = new AsyncSubject<DistributionList>();
            this._api
                .getDistributionListById(distributionListId)
                .subscribe((distributionList: DistributionList) => {
                    this._distributionList?.next(distributionList);
                    this._distributionList?.complete();
                });
        }
        return this._distributionList;
    }

    public updateDistributionList(distributionListId: string, distributionList: DistributionList): Observable<void> {
        const body = new UpdateDistributionListRequest({
            name: distributionList.name
        });
        return this._api
            .updateDistributionList(distributionListId, body)
            .pipe(
                map(_ => this._distributionList = undefined)
            );
    }
}
