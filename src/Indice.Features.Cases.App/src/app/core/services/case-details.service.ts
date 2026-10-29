import { Injectable } from '@angular/core';
import { ReplaySubject, Subject } from 'rxjs';
import { Case } from './cases-api.service';

@Injectable({
    providedIn: 'root'
})
export class CaseDetailsService {
    private _caseDetails: ReplaySubject<Case> = new ReplaySubject(1);
    private _caseChanged = new Subject<string>();
    public caseDetails$ = this._caseDetails.asObservable();
    /**
     * Emits the id of a case changed outside the details page (e.g. from a side pane), so the page refreshes it. An event,
     * not state, so it stays a Subject: a signal would replay the last change to every new subscriber.
     */
    public caseChanged$ = this._caseChanged.asObservable();
    public caseDetails: Case | undefined;

    constructor() { }

    public setCaseDetails(caseDetails: Case) {
        this.caseDetails = caseDetails;
        this._caseDetails.next(caseDetails);
    }

    public notifyCaseChanged(caseId: string) {
        this._caseChanged.next(caseId);
    }
}