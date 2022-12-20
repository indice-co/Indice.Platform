import { Injectable } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import { CaseDetails } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class CaseDetailsService {
    private _caseDetails: ReplaySubject<CaseDetails> = new ReplaySubject(1);
    public caseDetails$ = this._caseDetails.asObservable();

    constructor() { }

    public setCaseDetails(caseDetails: CaseDetails) {
        this._caseDetails.next(caseDetails);
    }
}