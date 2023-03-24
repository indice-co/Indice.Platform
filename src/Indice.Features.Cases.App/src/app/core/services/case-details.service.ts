import { Injectable } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import { Case } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class CaseDetailsService {
    private _caseDetails: ReplaySubject<Case> = new ReplaySubject(1);
    public caseDetails$ = this._caseDetails.asObservable();
    public caseDetails: Case | undefined;

    constructor() { }

    public setCaseDetails(caseDetails: Case) {
        this.caseDetails = caseDetails;
        this._caseDetails.next(caseDetails);
    }
}